using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Users;

public class UserEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task UpdateProfile_ChangesTheDisplayNameEverywhere()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);

        var response = await owner.Client.PatchAsJsonAsync("/me", new { displayName = "  Ada Lovelace  " });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var me = await owner.Client.GetFromJsonAsync<JsonElement>("/me");
        me.GetProperty("displayName").GetString().ShouldBe("Ada Lovelace");
        var members = await owner.Client.GetFromJsonAsync<JsonElement>($"/teams/{teamId}/members");
        members.EnumerateArray().ShouldHaveSingleItem().GetProperty("displayName").GetString().ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task UpdateProfile_EmptyName_ReturnsBadRequest()
    {
        var user = await _api.CreateUserAsync();

        var response = await user.Client.PatchAsJsonAsync("/me", new { displayName = " " });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
