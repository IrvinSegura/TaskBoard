using System.Net;

namespace TaskBoard.Api.FunctionalTests.Boards;

public class BoardEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    [Fact]
    public async Task GetBoard_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/boards/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // TODO: register two users, create a private board with one of them,
    // and assert the other gets 404 on GET /boards/{id} (the core "only board members" rule).
}
