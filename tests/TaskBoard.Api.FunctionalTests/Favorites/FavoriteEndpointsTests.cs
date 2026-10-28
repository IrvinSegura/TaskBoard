using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Favorites;

public class FavoriteEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task StarredTeamsAndBoards_AreListedWithTheirTeam()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.CreateBoardAsync(owner, teamId);

        (await owner.Client.PutAsync($"/me/favorites/teams/{teamId}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.PutAsync($"/me/favorites/boards/{boardId}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        // Starring again changes nothing.
        (await owner.Client.PutAsync($"/me/favorites/boards/{boardId}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var favorites = await owner.Client.GetFromJsonAsync<JsonElement>("/me/favorites");
        var team = favorites.GetProperty("teams").EnumerateArray().ShouldHaveSingleItem();
        team.GetProperty("id").GetGuid().ShouldBe(teamId);
        team.GetProperty("myRole").GetString().ShouldBe("Owner");
        var board = favorites.GetProperty("boards").EnumerateArray().ShouldHaveSingleItem();
        board.GetProperty("id").GetGuid().ShouldBe(boardId);
        board.GetProperty("teamId").GetGuid().ShouldBe(teamId);
    }

    [Fact]
    public async Task Unstar_RemovesTheFavoriteAndIsIdempotent()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await owner.Client.PutAsync($"/me/favorites/boards/{boardId}", null);

        (await owner.Client.DeleteAsync($"/me/favorites/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.DeleteAsync($"/me/favorites/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var favorites = await owner.Client.GetFromJsonAsync<JsonElement>("/me/favorites");
        favorites.GetProperty("boards").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Favorites_ArePerUser()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        await owner.Client.PutAsync($"/me/favorites/teams/{teamId}", null);

        var favorites = await member.Client.GetFromJsonAsync<JsonElement>("/me/favorites");

        favorites.GetProperty("teams").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task StarringABoardOrTeamYouCannotSee_IsNotFound()
    {
        var owner = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);

        (await outsider.Client.PutAsync($"/me/favorites/teams/{teamId}", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Client.PutAsync($"/me/favorites/boards/{boardId}", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchivedBoards_AreNotListed()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await owner.Client.PutAsync($"/me/favorites/boards/{boardId}", null);

        (await owner.Client.PostAsync($"/boards/{boardId}/archive", null)).IsSuccessStatusCode.ShouldBeTrue();

        var favorites = await owner.Client.GetFromJsonAsync<JsonElement>("/me/favorites");
        favorites.GetProperty("boards").EnumerateArray().ShouldBeEmpty();
    }
}
