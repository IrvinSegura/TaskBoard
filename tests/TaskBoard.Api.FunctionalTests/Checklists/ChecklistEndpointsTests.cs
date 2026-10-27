using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Checklists;

public class ChecklistEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task Checklist_ShowsItemsOnTheCardAndProgressOnTheBoard()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);

        var first = await AddItemAsync(owner, cardId, "Design");
        var second = await AddItemAsync(owner, cardId, "Build");
        var third = await AddItemAsync(owner, cardId, "Ship");
        (await owner.Client.PatchAsJsonAsync($"/cards/{cardId}/checklist/{first}", new { isDone = true }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.PatchAsJsonAsync($"/cards/{cardId}/checklist/{second}", new { text = "Build it" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.DeleteAsync($"/cards/{cardId}/checklist/{third}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var card = await owner.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}");
        var items = card.GetProperty("checklist").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("text").GetString()).ShouldBe(["Design", "Build it"]);
        items.Select(i => i.GetProperty("isDone").GetBoolean()).ShouldBe([true, false]);

        var board = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        var progress = board.GetProperty("columns")[0].GetProperty("cards")[0].GetProperty("checklist");
        progress.GetProperty("done").GetInt32().ShouldBe(1);
        progress.GetProperty("total").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task UpdateItem_WithNothingToChange_ReturnsBadRequest()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);
        var itemId = await AddItemAsync(owner, cardId, "Design");

        var response = await owner.Client.PatchAsJsonAsync($"/cards/{cardId}/checklist/{itemId}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddItem_ToAnArchivedCard_ReturnsBadRequest()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);
        (await owner.Client.PostAsync($"/cards/{cardId}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await owner.Client.PostAsJsonAsync($"/cards/{cardId}/checklist", new { text = "Late" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Cards.Archived");
    }

    private static async Task<Guid> AddItemAsync(TestUser user, Guid cardId, string text) =>
        await ApiDriver.ReadIdAsync(await user.Client.PostAsJsonAsync($"/cards/{cardId}/checklist", new { text }));
}
