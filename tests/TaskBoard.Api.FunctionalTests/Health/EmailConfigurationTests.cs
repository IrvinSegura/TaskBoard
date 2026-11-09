using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace TaskBoard.Api.FunctionalTests.Health;

/// <summary>
/// With email turned on, a missing setting stops the API at startup instead of losing emails later.
/// </summary>
public class EmailConfigurationTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    [Theory]
    [InlineData("Email:WebAppUrl", "", "Email:WebAppUrl")]
    [InlineData("Email:WebAppUrl", "javascript:alert(1)", "Email:WebAppUrl")]
    [InlineData("Email:Smtp:Host", "", "Email:Smtp:Host")]
    [InlineData("Email:FromAddress", "", "Email:FromAddress")]
    public void EmailOn_WithAMissingSetting_DoesNotStart(string key, string value, string reported)
    {
        var misconfigured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:Enabled", "true");
            builder.UseSetting("Email:WebAppUrl", "https://taskboard.test");
            builder.UseSetting("Email:FromAddress", "avisos@taskboard.test");
            builder.UseSetting("Email:Smtp:Host", "localhost");
            builder.UseSetting(key, value);
        });

        var failure = Should.Throw<OptionsValidationException>(() => misconfigured.CreateClient());

        failure.Message.ShouldContain(reported);
    }

    [Fact]
    public async Task EmailOn_WithEverySetting_Starts()
    {
        var configured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:Enabled", "true");
            builder.UseSetting("Email:WebAppUrl", "https://taskboard.test");
            builder.UseSetting("Email:FromAddress", "avisos@taskboard.test");
            builder.UseSetting("Email:Smtp:Host", "localhost");
        });

        var response = await configured.CreateClient().GetAsync("/health/live");

        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
