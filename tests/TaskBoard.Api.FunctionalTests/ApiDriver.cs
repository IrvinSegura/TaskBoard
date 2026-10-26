using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests;

public sealed record TestUser(Guid Id, HttpClient Client);

/// <summary>
/// Sets up users, teams and boards through the public API, so tests only state what they check.
/// </summary>
public sealed class ApiDriver(TaskBoardApiFactory factory)
{
    public async Task<TestUser> CreateUserAsync()
    {
        var anonymous = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!Passw0rd";

        var register = await anonymous.PostAsJsonAsync("/auth/register", new { email, displayName = "Test user", password });
        var userId = await ReadIdAsync(register);

        var login = await anonymous.PostAsJsonAsync("/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new TestUser(userId, client);
    }

    public async Task<Guid> CreateTeamAsync(TestUser owner, params TestUser[] members)
    {
        var teamId = await ReadIdAsync(await owner.Client.PostAsJsonAsync("/teams", new { name = "Team" }));

        foreach (var member in members)
        {
            var response = await owner.Client.PostAsJsonAsync(
                $"/teams/{teamId}/members", new { userId = member.Id, role = "Member" });
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        return teamId;
    }

    public async Task<Guid> CreateBoardAsync(TestUser owner, Guid teamId, string visibility = "Private") =>
        await ReadIdAsync(await owner.Client.PostAsJsonAsync(
            $"/teams/{teamId}/boards", new { name = "Board", visibility }));

    public async Task AddBoardMemberAsync(TestUser admin, Guid boardId, TestUser member, string role)
    {
        var response = await admin.Client.PostAsJsonAsync(
            $"/boards/{boardId}/members", new { userId = member.Id, role });
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    public async Task<Guid> CreateColumnAsync(TestUser user, Guid boardId, string name = "To do") =>
        await ReadIdAsync(await user.Client.PostAsJsonAsync($"/boards/{boardId}/columns", new { name }));

    public async Task<Guid> CreateCardAsync(TestUser user, Guid columnId, string title = "Card", string? description = null) =>
        await ReadIdAsync(await user.Client.PostAsJsonAsync($"/columns/{columnId}/cards", new { title, description }));

    public static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // Problem Details responses carry the domain error code in "title".
    public static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
}
