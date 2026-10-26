using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Health;

public class HealthEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    [Fact]
    public async Task Health_ReportsDatabaseAndStorageWithoutAToken()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        report.GetProperty("status").GetString().ShouldBe("Healthy");
        report.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ShouldBe(["database", "storage"], ignoreOrder: true);
    }

    [Fact]
    public async Task Live_AnswersWithoutCheckingDependencies()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        report.GetProperty("checks").EnumerateArray().ShouldBeEmpty();
    }
}
