using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Users;

public class EmailPreferencesEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    private static async Task<List<string>> ReadMutedAsync(TestUser user)
    {
        var body = await user.Client.GetFromJsonAsync<JsonElement>("/me/email-preferences");
        return [.. body.GetProperty("mutedTypes").EnumerateArray().Select(t => t.GetString()!)];
    }

    [Fact]
    public async Task Get_ANewUser_HasNothingMuted()
    {
        var user = await _api.CreateUserAsync();

        (await ReadMutedAsync(user)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Put_ReplacesWhatIsMuted()
    {
        var user = await _api.CreateUserAsync();

        var first = await user.Client.PutAsJsonAsync("/me/email-preferences", new { mutedTypes = new[] { "CommentAdded", "Mentioned" } });
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadMutedAsync(user)).ShouldBe(["CommentAdded", "Mentioned"]);

        await user.Client.PutAsJsonAsync("/me/email-preferences", new { mutedTypes = Array.Empty<string>() });
        (await ReadMutedAsync(user)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Put_ANumberThatIsNoType_ReturnsBadRequest()
    {
        var user = await _api.CreateUserAsync();

        var response = await user.Client.PutAsJsonAsync("/me/email-preferences", new { mutedTypes = new[] { 99 } });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Preferences_RequireASession()
    {
        var anonymous = factory.CreateClient();

        (await anonymous.GetAsync("/me/email-preferences")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssigningACard_EmailsTheAssigneeWithALinkToTheCard()
    {
        var (owner, assignee, boardId, cardId) = await GivenACardAsync("Ship it");

        await AssignAsync(owner, cardId, assignee);

        var email = await factory.Emails.WaitForAsync(assignee.Email, "Ship it");
        email.Subject.ShouldBe("Te asignaron la tarjeta «Ship it»");
        email.TextBody.ShouldContain($"/boards/{boardId}/cards/{cardId}");
    }

    [Fact]
    public async Task AssigningACard_WhenThatEmailIsMuted_KeepsTheNotificationButSendsNoEmail()
    {
        var (owner, assignee, _, cardId) = await GivenACardAsync("Quiet card");
        await assignee.Client.PutAsJsonAsync("/me/email-preferences", new { mutedTypes = new[] { "CardAssigned" } });

        await AssignAsync(owner, cardId, assignee);

        await WaitForNotificationAsync(assignee, cardId);
        factory.Emails.To(assignee.Email).ShouldNotContain(m => m.Subject.Contains("Quiet card"));
    }

    [Fact]
    public async Task Commenting_EmailsTheAssigneeAndNamesTheAuthor()
    {
        var (owner, assignee, _, cardId) = await GivenACardAsync("Discussed card");
        await AssignAsync(owner, cardId, assignee);

        var comment = await owner.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body = "Looks good" });
        comment.IsSuccessStatusCode.ShouldBeTrue(await comment.Content.ReadAsStringAsync());

        var email = await factory.Emails.WaitForAsync(assignee.Email, "comentó");
        email.Subject.ShouldBe("Test user comentó en «Discussed card»");

        // The comment's text stays in the app, behind the board's permissions.
        email.TextBody.ShouldNotContain("Looks good");
    }

    [Fact]
    public async Task AddingToATeam_EmailsTheNewMemberButNotTheOwner()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();

        await _api.CreateTeamAsync(owner, member);

        await factory.Emails.WaitForAsync(member.Email, "equipo");
        factory.Emails.To(owner.Email).ShouldNotContain(m => m.Subject.Contains("equipo"));
    }

    [Fact]
    public async Task AddingToABoard_EmailsTheNewMember()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        var boardId = await _api.CreateBoardAsync(owner, teamId);

        await _api.AddBoardMemberAsync(owner, boardId, member, "Viewer");

        var email = await factory.Emails.WaitForAsync(member.Email, "tablero");
        email.TextBody.ShouldContain($"/boards/{boardId}");
    }

    private async Task<(TestUser Owner, TestUser Assignee, Guid BoardId, Guid CardId)> GivenACardAsync(string title)
    {
        var owner = await _api.CreateUserAsync();
        var assignee = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, assignee);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, assignee, "Editor");
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, title);
        return (owner, assignee, boardId, cardId);
    }

    private static async Task AssignAsync(TestUser owner, Guid cardId, TestUser assignee)
    {
        var assign = await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = assignee.Id });
        assign.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task WaitForNotificationAsync(TestUser user, Guid resourceId)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            var notifications = await user.Client.GetFromJsonAsync<JsonElement>("/notifications");
            if (notifications.EnumerateArray().Any(n =>
                    n.GetProperty("resourceId") is { ValueKind: JsonValueKind.String } id && id.GetGuid() == resourceId))
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"No notification for {resourceId} arrived.");
    }
}
