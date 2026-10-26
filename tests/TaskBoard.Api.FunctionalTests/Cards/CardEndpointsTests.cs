using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Cards;

public class CardEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task GetCard_ReturnsDetailWithAssignees()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, "Write docs", "Explain the setup");
        await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = owner.Id });

        var card = await owner.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}");

        card.GetProperty("title").GetString().ShouldBe("Write docs");
        card.GetProperty("description").GetString().ShouldBe("Explain the setup");
        card.GetProperty("columnId").GetGuid().ShouldBe(columnId);
        card.GetProperty("assignees").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("userId").GetGuid().ShouldBe(owner.Id);
    }

    [Fact]
    public async Task UpdateCard_ChangesFieldsReturnedByDetail()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);

        var update = await owner.Client.PutAsJsonAsync(
            $"/cards/{cardId}", new { title = "Renamed", description = (string?)null, priority = "Urgent", dueDate = (DateTimeOffset?)null });
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var card = await owner.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}");
        card.GetProperty("title").GetString().ShouldBe("Renamed");
        card.GetProperty("priority").GetString().ShouldBe("Urgent");
    }

    [Fact]
    public async Task SearchCards_CombinesTextPriorityAndArchivedFilters()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var loginBug = await _api.CreateCardAsync(owner, columnId, "Fix LOGIN bug");
        var loginDocs = await _api.CreateCardAsync(owner, columnId, "Document login flow");
        await _api.CreateCardAsync(owner, columnId, "Unrelated");
        await owner.Client.PutAsJsonAsync(
            $"/cards/{loginBug}", new { title = "Fix LOGIN bug", priority = "High" });
        await owner.Client.PostAsync($"/cards/{loginDocs}/archive", null);

        var byText = await SearchAsync(owner, boardId, "text=login");
        byText.ShouldBe([loginBug]);

        var byPriority = await SearchAsync(owner, boardId, "priority=High");
        byPriority.ShouldBe([loginBug]);

        var archived = await SearchAsync(owner, boardId, "text=login&archived=true");
        archived.ShouldBe([loginDocs]);
    }

    [Fact]
    public async Task ArchiveAndRestoreCard_RemovesItFromTheBoardAndPutsItBack()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);

        (await owner.Client.PostAsync($"/cards/{cardId}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetBoardCardIdsAsync(owner, boardId)).ShouldBeEmpty();

        (await owner.Client.PostAsync($"/cards/{cardId}/restore", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetBoardCardIdsAsync(owner, boardId)).ShouldBe([cardId]);
    }

    [Fact]
    public async Task MoveColumn_ReordersColumnsOnTheBoard()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var todo = await _api.CreateColumnAsync(owner, boardId, "To do");
        var doing = await _api.CreateColumnAsync(owner, boardId, "Doing");
        var done = await _api.CreateColumnAsync(owner, boardId, "Done");

        var move = await owner.Client.PatchAsJsonAsync(
            $"/columns/{done}/move", new { previousColumnId = (Guid?)null, nextColumnId = todo });
        move.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var board = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        board.GetProperty("columns").EnumerateArray().Select(c => c.GetProperty("id").GetGuid())
            .ShouldBe([done, todo, doing]);
    }

    private static async Task<List<Guid>> SearchAsync(TestUser user, Guid boardId, string queryString)
    {
        var cards = await user.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}/cards?{queryString}");
        return cards.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
    }

    private static async Task<List<Guid>> GetBoardCardIdsAsync(TestUser user, Guid boardId)
    {
        var board = await user.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        return board.GetProperty("columns").EnumerateArray()
            .SelectMany(c => c.GetProperty("cards").EnumerateArray())
            .Select(c => c.GetProperty("id").GetGuid())
            .ToList();
    }
}
