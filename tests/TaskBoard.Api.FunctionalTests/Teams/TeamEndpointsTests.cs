using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Teams;

public class TeamEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task GetMembers_ListsOwnerFirstWithEmailAndRole()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);

        var members = (await member.Client.GetFromJsonAsync<JsonElement>($"/teams/{teamId}/members"))
            .EnumerateArray().ToList();

        members.Count.ShouldBe(2);
        members[0].GetProperty("userId").GetGuid().ShouldBe(owner.Id);
        members[0].GetProperty("email").GetString().ShouldBe(owner.Email);
        members[0].GetProperty("role").GetString().ShouldBe("Owner");
        members[1].GetProperty("userId").GetGuid().ShouldBe(member.Id);
        members[1].GetProperty("role").GetString().ShouldBe("Member");
    }

    [Fact]
    public async Task GetMembers_IsNotFoundForUsersOutsideTheTeam()
    {
        var owner = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);

        var response = await outsider.Client.GetAsync($"/teams/{teamId}/members");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMember_MatchesTheEmailIgnoringCaseAndSpaces()
    {
        var owner = await _api.CreateUserAsync();
        var invitee = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);

        var response = await owner.Client.PostAsJsonAsync(
            $"/teams/{teamId}/members", new { email = $" {invitee.Email.ToUpperInvariant()} ", role = "Admin" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var members = await owner.Client.GetFromJsonAsync<JsonElement>($"/teams/{teamId}/members");
        members.EnumerateArray().ShouldContain(m =>
            m.GetProperty("userId").GetGuid() == invitee.Id && m.GetProperty("role").GetString() == "Admin");
    }

    [Fact]
    public async Task AddMember_UnknownEmail_ReturnsNotFound()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);

        var response = await owner.Client.PostAsJsonAsync(
            $"/teams/{teamId}/members", new { email = $"nobody-{Guid.NewGuid():N}@example.com", role = "Member" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Users.NotFoundByEmail");
    }

    [Fact]
    public async Task AddMember_ByPlainMember_IsForbiddenWhetherOrNotTheEmailExists()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var registered = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);

        var known = await member.Client.PostAsJsonAsync(
            $"/teams/{teamId}/members", new { email = registered.Email, role = "Member" });
        var unknown = await member.Client.PostAsJsonAsync(
            $"/teams/{teamId}/members", new { email = $"nobody-{Guid.NewGuid():N}@example.com", role = "Member" });

        known.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RenameTeam_ByAdmin_ChangesTheNameInTheTeamList()
    {
        var owner = await _api.CreateUserAsync();
        var admin = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        (await owner.Client.PostAsJsonAsync($"/teams/{teamId}/members", new { email = admin.Email, role = "Admin" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await admin.Client.PatchAsJsonAsync($"/teams/{teamId}", new { name = "Renamed" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var teams = await owner.Client.GetFromJsonAsync<JsonElement>("/teams");
        teams.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == teamId)
            .GetProperty("name").GetString().ShouldBe("Renamed");
    }

    [Fact]
    public async Task RenameTeam_ByPlainMember_IsForbidden()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);

        var response = await member.Client.PatchAsJsonAsync($"/teams/{teamId}", new { name = "Renamed" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTeam_ByOwner_RemovesTheTeamAndItsBoards()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        var boardId = await _api.CreateBoardAsync(owner, teamId, visibility: "Team");
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        await _api.CreateCardAsync(owner, columnId);

        var response = await owner.Client.DeleteAsync($"/teams/{teamId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await member.Client.GetFromJsonAsync<JsonElement>("/teams")).EnumerateArray().ShouldBeEmpty();
        (await owner.Client.GetAsync($"/boards/{boardId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTeam_ByAdmin_IsForbidden()
    {
        var owner = await _api.CreateUserAsync();
        var admin = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        (await owner.Client.PostAsJsonAsync($"/teams/{teamId}/members", new { email = admin.Email, role = "Admin" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await admin.Client.DeleteAsync($"/teams/{teamId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Teams.OnlyOwnerCanDelete");
    }

    [Fact]
    public async Task AddMember_InvalidEmail_ReturnsBadRequest()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);

        var response = await owner.Client.PostAsJsonAsync(
            $"/teams/{teamId}/members", new { email = "not-an-email", role = "Member" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
