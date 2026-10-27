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

    [Fact]
    public async Task SearchCards_ReturnsPagesWithATotalCount()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardIds = new List<Guid>();
        foreach (var title in new[] { "A", "B", "C" })
        {
            cardIds.Add(await _api.CreateCardAsync(owner, columnId, title));
        }

        var secondPage = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}/cards?page=2&pageSize=2");

        secondPage.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([cardIds[2]]);
        secondPage.GetProperty("page").GetInt32().ShouldBe(2);
        secondPage.GetProperty("totalCount").GetInt32().ShouldBe(3);
        secondPage.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task MoveCard_RepeatedlyBetweenTheSameNeighbours_KeepsTheOrderAfterRenumbering()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var first = await _api.CreateCardAsync(owner, columnId, "First");
        var last = await _api.CreateCardAsync(owner, columnId, "Last");
        var a = await _api.CreateCardAsync(owner, columnId, "A");
        var b = await _api.CreateCardAsync(owner, columnId, "B");

        // Each move lands right after "First", in front of the card moved before it, halving the same gap
        // every time. Well past the ~30 splits a gap allows, so the column has to be renumbered on the way.
        var expected = new List<Guid> { first, last, a, b };
        for (var i = 0; i < 60; i++)
        {
            var moving = i % 2 == 0 ? b : a;
            var next = expected[1];
            var move = await owner.Client.PatchAsJsonAsync(
                $"/cards/{moving}/move", new { targetColumnId = columnId, previousCardId = first, nextCardId = next });
            move.StatusCode.ShouldBe(HttpStatusCode.OK, await move.Content.ReadAsStringAsync());

            expected.Remove(moving);
            expected.Insert(1, moving);
        }

        var board = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        var cards = board.GetProperty("columns")[0].GetProperty("cards").EnumerateArray().ToList();
        cards.Select(c => c.GetProperty("id").GetGuid()).ShouldBe(expected);

        var positions = cards.Select(c => c.GetProperty("position").GetDouble()).ToList();
        positions.Zip(positions.Skip(1), (previous, next) => next - previous).ShouldAllBe(gap => gap > 0);
    }

    private static async Task<List<Guid>> SearchAsync(TestUser user, Guid boardId, string queryString)
    {
        var cards = await user.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}/cards?{queryString}");
        return cards.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
    }

    [Fact]
    public async Task DeleteCard_OnlyOnceArchived_RemovesItForGood()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId);
        (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body = "Goes with the card" }))
            .IsSuccessStatusCode.ShouldBeTrue();

        var active = await owner.Client.DeleteAsync($"/cards/{cardId}");
        active.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(active)).ShouldBe("Cards.NotArchived");

        (await owner.Client.PostAsync($"/cards/{cardId}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.DeleteAsync($"/cards/{cardId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await owner.Client.GetAsync($"/cards/{cardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteColumn_DeletesItsCardsArchivedOnesIncluded()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId, "Doomed");
        var keptColumnId = await _api.CreateColumnAsync(owner, boardId, "Kept");
        var activeCardId = await _api.CreateCardAsync(owner, columnId);
        var archivedCardId = await _api.CreateCardAsync(owner, columnId);
        var keptCardId = await _api.CreateCardAsync(owner, keptColumnId);
        (await owner.Client.PostAsync($"/cards/{archivedCardId}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await owner.Client.DeleteAsync($"/columns/{columnId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var board = await owner.Client.GetFromJsonAsync<JsonElement>($"/boards/{boardId}");
        board.GetProperty("columns").EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetGuid().ShouldBe(keptColumnId);
        (await GetBoardCardIdsAsync(owner, boardId)).ShouldBe([keptCardId]);
        (await owner.Client.GetAsync($"/cards/{activeCardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await owner.Client.GetAsync($"/cards/{archivedCardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteColumn_ByEditor_IsForbidden()
    {
        var owner = await _api.CreateUserAsync();
        var editor = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, editor);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, editor, "Editor");
        var columnId = await _api.CreateColumnAsync(owner, boardId);

        var response = await editor.Client.DeleteAsync($"/columns/{columnId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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
