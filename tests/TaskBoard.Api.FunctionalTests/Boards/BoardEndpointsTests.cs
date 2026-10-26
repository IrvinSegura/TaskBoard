using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Boards;

public class BoardEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task GetBoard_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/boards/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBoard_PrivateBoard_IsNotFoundForUsersWhoAreNotBoardMembers()
    {
        var owner = await _api.CreateUserAsync();
        var teammate = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, teammate);
        var boardId = await _api.CreateBoardAsync(owner, teamId, visibility: "Private");

        (await owner.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await teammate.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoard_TeamBoard_IsReadOnlyForTeamMembers()
    {
        var owner = await _api.CreateUserAsync();
        var teammate = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, teammate);
        var boardId = await _api.CreateBoardAsync(owner, teamId, visibility: "Team");

        (await teammate.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var write = await teammate.Client.PostAsJsonAsync($"/boards/{boardId}/columns", new { name = "Mine" });
        write.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArchivedBoard_RejectsWritesButStaysReadableUntilRestored()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);

        (await owner.Client.PostAsync($"/boards/{boardId}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var createCard = await owner.Client.PostAsJsonAsync($"/columns/{columnId}/cards", new { title = "Late" });
        createCard.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(createCard)).ShouldBe("Boards.Archived");

        (await owner.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var activeBoards = await owner.Client.GetFromJsonAsync<JsonElement>($"/teams/{teamId}/boards");
        activeBoards.EnumerateArray().ShouldBeEmpty();
        var archivedBoards = await owner.Client.GetFromJsonAsync<JsonElement>($"/teams/{teamId}/boards?archived=true");
        archivedBoards.EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetGuid().ShouldBe(boardId);

        (await owner.Client.PostAsync($"/boards/{boardId}/restore", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await _api.CreateCardAsync(owner, columnId, "After restore");
    }

    [Fact]
    public async Task RemoveBoardMember_RemovedUserLosesAccess()
    {
        var owner = await _api.CreateUserAsync();
        var editor = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, editor);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, editor, "Editor");

        (await editor.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var remove = await owner.Client.DeleteAsync($"/boards/{boardId}/members/{editor.Id}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await editor.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
