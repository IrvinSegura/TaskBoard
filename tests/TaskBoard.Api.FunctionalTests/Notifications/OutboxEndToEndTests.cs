using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Notifications;

/// <summary>
/// Goes through the whole outbox: the command saves an event, the background processor dispatches it,
/// and its handler stores a notification for the assignee.
/// </summary>
public class OutboxEndToEndTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task AssigningACard_NotifiesTheAssigneeThroughTheOutbox()
    {
        var owner = await _api.CreateUserAsync();
        var assignee = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, assignee);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, assignee, "Editor");
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, "Ship it");

        var assign = await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = assignee.Id });
        assign.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var notification = await WaitForNotificationAsync(assignee, cardId);
        notification.GetProperty("type").GetString().ShouldBe("CardAssigned");
        notification.GetProperty("message").GetString()!.ShouldContain("Ship it");
    }

    // The handler runs asynchronously after the request, so poll until the notification shows up.
    private static async Task<JsonElement> WaitForNotificationAsync(TestUser user, Guid resourceId)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            var notifications = await user.Client.GetFromJsonAsync<JsonElement>("/notifications");
            foreach (var notification in notifications.EnumerateArray())
            {
                if (notification.GetProperty("resourceId") is { ValueKind: JsonValueKind.String } id &&
                    id.GetGuid() == resourceId)
                {
                    return notification;
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"No notification for {resourceId} arrived within {Timeout}.");
    }
}
