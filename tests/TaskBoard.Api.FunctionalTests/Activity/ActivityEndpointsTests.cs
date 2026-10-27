using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Activity;

/// <summary>
/// Activity and comment notifications are written by outbox handlers after the request, so these tests poll.
/// </summary>
public class ActivityEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task BoardActivity_RecordsWhoDidWhatNewestFirst()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var todo = await _api.CreateColumnAsync(owner, boardId, "To do");
        var done = await _api.CreateColumnAsync(owner, boardId, "Done");
        var cardId = await _api.CreateCardAsync(owner, todo, "Ship it");

        var move = await owner.Client.PatchAsJsonAsync(
            $"/cards/{cardId}/move", new { targetColumnId = done, previousCardId = (Guid?)null, nextCardId = (Guid?)null });
        move.IsSuccessStatusCode.ShouldBeTrue();

        var entries = await WaitForAsync(owner, $"/boards/{boardId}/activity", count: 4);
        entries.Select(e => e.GetProperty("type").GetString()).ShouldBe(["CardMoved", "CardCreated", "ColumnCreated", "ColumnCreated"]);
        entries[0].GetProperty("subject").GetString().ShouldBe("Ship it");
        entries[0].GetProperty("detail").GetString().ShouldBe("Done");
        entries[0].GetProperty("actorId").GetGuid().ShouldBe(owner.Id);
        entries[0].GetProperty("actorName").GetString().ShouldBe("Test user");

        var cardEntries = await WaitForAsync(owner, $"/cards/{cardId}/activity", count: 2);
        cardEntries.ShouldAllBe(e => e.GetProperty("cardId").GetGuid() == cardId);
    }

    [Fact]
    public async Task MyActivity_OnlyShowsBoardsTheUserCanSee()
    {
        var owner = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.CreateColumnAsync(owner, boardId);

        await WaitForAsync(owner, "/me/activity", count: 1);

        var outsiderFeed = await outsider.Client.GetFromJsonAsync<JsonElement>("/me/activity");
        outsiderFeed.GetProperty("items").EnumerateArray().ShouldBeEmpty();
        (await outsider.Client.GetAsync($"/boards/{boardId}/activity")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Client.GetAsync($"/teams/{teamId}/activity")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Comment_NotifiesMentionsOnceAndOtherAssignees()
    {
        var owner = await _api.CreateUserAsync();
        var mentioned = await _api.CreateUserAsync();
        var assignee = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, mentioned, assignee);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        await _api.AddBoardMemberAsync(owner, boardId, mentioned, "Editor");
        await _api.AddBoardMemberAsync(owner, boardId, assignee, "Editor");
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, "Release notes");
        foreach (var user in new[] { mentioned, assignee })
        {
            (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = user.Id }))
                .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        var body = $"@[Someone](user:{mentioned.Id}) can you review?";
        (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body })).IsSuccessStatusCode.ShouldBeTrue();

        var mention = await WaitForNotificationAsync(mentioned, "Mentioned");
        mention.GetProperty("resourceId").GetGuid().ShouldBe(cardId);
        mention.GetProperty("message").GetString()!.ShouldContain("Release notes");
        await WaitForNotificationAsync(assignee, "CommentAdded");

        // The mentioned assignee gets the mention, not a second "commented" notification.
        var mentionedTypes = (await mentioned.Client.GetFromJsonAsync<JsonElement>("/notifications"))
            .EnumerateArray().Select(n => n.GetProperty("type").GetString()).ToList();
        mentionedTypes.ShouldNotContain("CommentAdded");

        var activity = await WaitForAsync(owner, $"/cards/{cardId}/activity", count: 4);
        activity[0].GetProperty("detail").GetString().ShouldBe("@Someone can you review?");
    }

    private static async Task<List<JsonElement>> WaitForAsync(TestUser user, string url, int count)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            var page = await user.Client.GetFromJsonAsync<JsonElement>(url);
            var items = page.GetProperty("items").EnumerateArray().ToList();
            if (items.Count >= count || DateTime.UtcNow > deadline)
            {
                items.Count.ShouldBeGreaterThanOrEqualTo(count, $"{url} returned {items.Count} entries");
                return items;
            }

            await Task.Delay(200);
        }
    }

    private static async Task<JsonElement> WaitForNotificationAsync(TestUser user, string type)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            var notifications = await user.Client.GetFromJsonAsync<JsonElement>("/notifications");
            foreach (var notification in notifications.EnumerateArray())
            {
                if (notification.GetProperty("type").GetString() == type)
                {
                    return notification;
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"No {type} notification arrived within {Timeout}.");
    }
}
