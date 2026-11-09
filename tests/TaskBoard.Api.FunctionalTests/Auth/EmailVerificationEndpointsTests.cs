using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TaskBoard.Api.FunctionalTests.Auth;

public class EmailVerificationEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private readonly ApiDriver _api = new(factory);

    private static async Task<bool> IsVerifiedAsync(TestUser user) =>
        (await user.Client.GetFromJsonAsync<JsonElement>("/me")).GetProperty("emailVerified").GetBoolean();

    private static string TokenIn(string text) =>
        Regex.Match(text, "/verify-email#token=([A-Za-z0-9_-]+)").Groups[1].Value;

    [Fact]
    public async Task SigningUp_SendsAWelcomeWhoseLinkConfirmsTheEmail()
    {
        var user = await _api.CreateUserAsync();
        (await IsVerifiedAsync(user)).ShouldBeFalse();

        var welcome = await factory.Emails.WaitForAsync(user.Email, "bienvenida");
        var verify = await factory.CreateClient().PostAsJsonAsync("/auth/verify-email", new { token = TokenIn(welcome.TextBody) });

        verify.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await IsVerifiedAsync(user)).ShouldBeTrue();
    }

    [Fact]
    public async Task Verify_AForgedToken_IsRejected()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/verify-email", new { token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Users.InvalidEmailVerificationToken");
    }

    [Fact]
    public async Task AskingAgain_SendsANewLinkUntilTheEmailIsConfirmed()
    {
        var user = await _api.CreateUserAsync();

        (await user.Client.PostAsync("/me/verification-email", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var again = await factory.Emails.WaitForAsync(user.Email, "Confirma tu correo");
        await factory.CreateClient().PostAsJsonAsync("/auth/verify-email", new { token = TokenIn(again.TextBody) });

        (await user.Client.PostAsync("/me/verification-email", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        factory.Emails.To(user.Email).Count(m => m.Subject.Contains("Confirma tu correo")).ShouldBe(1);
    }
}
