using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Labels;

public class LabelEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task Labels_AreListedOnTheBoardAndOnTheirCards()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);

        var labelId = await ApiDriver.ReadIdAsync(
            await owner.Client.PostAsJsonAsync($"/boards/{boardId}/labels", new { name = "Bug", color = "Red" }));
        (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/labels", new { labelId }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var board = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        var label = board.GetProperty("labels").EnumerateArray().ShouldHaveSingleItem();
        label.GetProperty("name").GetString().ShouldBe("Bug");
        label.GetProperty("color").GetString().ShouldBe("Red");
        var card = board.GetProperty("columns")[0].GetProperty("cards")[0];
        card.GetProperty("labelIds").EnumerateArray().Select(l => l.GetGuid()).ShouldBe([labelId]);

        var search = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}/cards?labelId={labelId}");
        search.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetGuid().ShouldBe(cardId);
    }

    [Fact]
    public async Task DeleteLabel_RemovesItFromItsCards()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);
        var labelId = await ApiDriver.ReadIdAsync(
            await owner.Client.PostAsJsonAsync($"/boards/{boardId}/labels", new { name = "Bug", color = "Red" }));
        await owner.Client.PostAsJsonAsync($"/cards/{cardId}/labels", new { labelId });

        (await owner.Client.DeleteAsync($"/boards/{boardId}/labels/{labelId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var card = await owner.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}");
        card.GetProperty("labelIds").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task AddCardLabel_LabelOfAnotherBoard_ReturnsBadRequest()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var otherBoardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);
        var foreignLabelId = await ApiDriver.ReadIdAsync(
            await owner.Client.PostAsJsonAsync($"/boards/{otherBoardId}/labels", new { name = "Bug", color = "Red" }));

        var response = await owner.Client.PostAsJsonAsync($"/cards/{cardId}/labels", new { labelId = foreignLabelId });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Cards.LabelNotOnBoard");
    }

    [Fact]
    public async Task UpdateLabel_ThroughAnotherBoard_IsNotFound()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var otherBoardId = await _api.CreateBoardAsync(owner, teamId);
        var labelId = await ApiDriver.ReadIdAsync(
            await owner.Client.PostAsJsonAsync($"/boards/{boardId}/labels", new { name = "Bug", color = "Red" }));

        var response = await owner.Client.PutAsJsonAsync(
            $"/boards/{otherBoardId}/labels/{labelId}", new { name = "Hijacked", color = "Blue" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateLabel_ByViewer_IsForbidden()
    {
        var owner = await _api.CreateUserAsync();
        var viewer = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, viewer);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, viewer, "Viewer");

        var response = await viewer.Client.PostAsJsonAsync($"/boards/{boardId}/labels", new { name = "Bug", color = "Red" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
