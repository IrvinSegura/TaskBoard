using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Calendar;

public class CalendarEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task Calendar_ListsCardsDueInTheRangeAndItsActivity()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var dueCardId = await _api.CreateCardAsync(owner, columnId, "Due soon");
        var laterCardId = await _api.CreateCardAsync(owner, columnId, "Due later");
        await _api.CreateCardAsync(owner, columnId, "No date");

        var now = DateTimeOffset.UtcNow;
        await SetDueDateAsync(owner, dueCardId, "Due soon", now.AddDays(2));
        await SetDueDateAsync(owner, laterCardId, "Due later", now.AddDays(60));

        var calendar = await WaitForActivityAsync(owner, now.AddDays(-1), now.AddDays(30), count: 1);

        var card = calendar.GetProperty("cards").EnumerateArray().ShouldHaveSingleItem();
        card.GetProperty("id").GetGuid().ShouldBe(dueCardId);
        card.GetProperty("boardName").GetString().ShouldNotBeNullOrEmpty();
        card.GetProperty("columnName").GetString().ShouldBe("To do");
        card.GetProperty("isAssignedToMe").GetBoolean().ShouldBeFalse();
        calendar.GetProperty("activity").EnumerateArray().ShouldAllBe(a => a.GetProperty("boardId").GetGuid() == boardId);
        calendar.GetProperty("activityTruncated").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Calendar_HidesBoardsTheUserCannotSee()
    {
        var owner = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId);
        var cardId = await _api.CreateCardAsync(owner, columnId, "Secret");
        var now = DateTimeOffset.UtcNow;
        await SetDueDateAsync(owner, cardId, "Secret", now.AddDays(1));

        var calendar = await GetCalendarAsync(outsider, now.AddDays(-1), now.AddDays(7));

        calendar.GetProperty("cards").EnumerateArray().ShouldBeEmpty();
        calendar.GetProperty("activity").EnumerateArray().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(60)]
    public async Task Calendar_InvalidRange_ReturnsBadRequest(int days)
    {
        var user = await _api.CreateUserAsync();
        var from = DateTimeOffset.UtcNow;

        var response = await user.Client.GetAsync(CalendarUrl(from, from.AddDays(days)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Calendar.InvalidRange");
    }

    private static async Task SetDueDateAsync(TestUser user, Guid cardId, string title, DateTimeOffset dueDate)
    {
        var response = await user.Client.PutAsJsonAsync(
            $"/cards/{cardId}", new { title, description = (string?)null, priority = "None", dueDate });
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    private static string CalendarUrl(DateTimeOffset from, DateTimeOffset to) =>
        $"/me/calendar?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

    private static Task<JsonElement> GetCalendarAsync(TestUser user, DateTimeOffset from, DateTimeOffset to) =>
        user.Client.GetFromJsonAsync<JsonElement>(CalendarUrl(from, to));

    // Activity is written by outbox handlers after the request, so poll for it.
    private static async Task<JsonElement> WaitForActivityAsync(TestUser user, DateTimeOffset from, DateTimeOffset to, int count)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            var calendar = await GetCalendarAsync(user, from, to);
            var found = calendar.GetProperty("activity").GetArrayLength();
            if (found >= count || DateTime.UtcNow > deadline)
            {
                found.ShouldBeGreaterThanOrEqualTo(count, $"the calendar returned {found} activity entries");
                return calendar;
            }

            await Task.Delay(200);
        }
    }
}
