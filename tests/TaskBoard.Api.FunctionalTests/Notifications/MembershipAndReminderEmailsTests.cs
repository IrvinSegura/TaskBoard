using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Features.Cards.DueReminders;

namespace TaskBoard.Api.FunctionalTests.Notifications;

public class MembershipAndReminderEmailsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task RemovingSomeoneFromAPrivateBoard_TellsThem()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, member, "Editor");

        var response = await owner.Client.DeleteAsync($"/boards/{boardId}/members/{member.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var email = await factory.Emails.WaitForAsync(member.Email, "Ya no estás en el tablero");
        email.TextBody.ShouldContain("Test user te quitó del tablero «Board»");
    }

    [Fact]
    public async Task RemovingSomeoneFromATeam_TellsThemOnceAndNotAboutEachBoard()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, member, "Viewer");

        (await owner.Client.DeleteAsync($"/teams/{teamId}/members/{member.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await factory.Emails.WaitForAsync(member.Email, "Ya no estás en el equipo");
        factory.Emails.To(member.Email).ShouldNotContain(m => m.Subject.Contains("Ya no estás en el tablero"));
    }

    [Fact]
    public async Task LeavingATeam_TellsNoOne()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);

        (await member.Client.DeleteAsync($"/teams/{teamId}/members/{member.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await Task.Delay(TimeSpan.FromSeconds(2));

        factory.Emails.To(member.Email).ShouldNotContain(m => m.Subject.Contains("Ya no estás"));
    }

    [Fact]
    public async Task ChangingSomeonesTeamRole_TellsThemTheNewRole()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);

        var response = await owner.Client.PatchAsJsonAsync($"/teams/{teamId}/members/{member.Id}", new { role = "Admin" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var email = await factory.Emails.WaitForAsync(member.Email, "Ahora eres administrador");
        email.TextBody.ShouldContain($"/teams/{teamId}");
    }

    [Fact]
    public async Task ACardDueWithinADay_RemindsItsAssigneeOnce()
    {
        var owner = await _api.CreateUserAsync();
        var assignee = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, assignee);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, assignee, "Editor");
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, "Entregar el reporte");
        (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = assignee.Id }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.Client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            title = "Entregar el reporte",
            priority = "None",
            dueDate = DateTimeOffset.UtcNow.AddHours(3)
        })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await SendRemindersAsync()).ShouldBe(1);
        (await SendRemindersAsync()).ShouldBe(0);

        var email = await factory.Emails.WaitForAsync(assignee.Email, "vence pronto");
        email.TextBody.ShouldContain($"/boards/{boardId}/cards/{cardId}");
    }

    private async Task<int> SendRemindersAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICardDueReminders>().SendAsync(CancellationToken.None);
    }
}
