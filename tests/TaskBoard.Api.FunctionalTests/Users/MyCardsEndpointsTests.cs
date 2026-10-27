using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Users;

public class MyCardsEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task GetMyCards_ListsAssignedActiveCardsSoonestFirst()
    {
        var owner = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner);
        var boardId = await _api.CreateBoardAsync(owner, teamId);
        var columnId = await _api.CreateColumnAsync(owner, boardId, "Doing");
        var later = await _api.CreateCardAsync(owner, columnId, "Later");
        var soon = await _api.CreateCardAsync(owner, columnId, "Soon");
        var archived = await _api.CreateCardAsync(owner, columnId, "Archived");
        await _api.CreateCardAsync(owner, columnId, "Not mine");

        foreach (var (cardId, days) in new[] { (later, 10), (soon, 1), (archived, 2) })
        {
            await owner.Client.PutAsJsonAsync($"/cards/{cardId}", new
            {
                title = "Card",
                description = (string?)null,
                priority = "High",
                dueDate = DateTimeOffset.UtcNow.AddDays(days)
            });
            (await owner.Client.PostAsJsonAsync($"/cards/{cardId}/assignees", new { userId = owner.Id }))
                .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        (await owner.Client.PostAsync($"/cards/{archived}/archive", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cards = (await owner.Client.GetFromJsonAsync<JsonElement>("/me/cards")).EnumerateArray().ToList();

        cards.Select(c => c.GetProperty("id").GetGuid()).ShouldBe([soon, later]);
        cards[0].GetProperty("columnName").GetString().ShouldBe("Doing");
        cards[0].GetProperty("teamName").GetString().ShouldBe("Team");
    }
}
