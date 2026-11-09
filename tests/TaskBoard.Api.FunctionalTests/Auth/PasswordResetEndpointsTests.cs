using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Api.Authentication;

namespace TaskBoard.Api.FunctionalTests.Auth;

public class PasswordResetEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private const string Password = "Passw0rd!Passw0rd";
    private const string NewPassword = "Another-Passw0rd!";

    private readonly HttpClient _anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    private readonly RecordingEmailSender _emails = factory.Emails;

    [Fact]
    public async Task Forgot_AnswersTheSameWhetherTheAccountExistsOrNot()
    {
        var email = await RegisterAsync();
        var unknown = $"nobody-{Guid.NewGuid():N}@example.com";

        var known = await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email });
        var missing = await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email = unknown });

        known.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        missing.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await known.Content.ReadAsStringAsync()).ShouldBe(await missing.Content.ReadAsStringAsync());

        ResetEmailsTo(email).ShouldBe(1);
        _emails.To(unknown).ShouldBeEmpty();
    }

    [Fact]
    public async Task Forgot_FindsTheAccountWhateverTheCaseOfTheAddress()
    {
        var email = await RegisterAsync();

        await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email = email.ToUpperInvariant() });

        ResetEmailsTo(email).ShouldBe(1);
    }

    [Fact]
    public async Task Forgot_NotAnAddress_ReturnsBadRequest()
    {
        var response = await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email = "not-an-address" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Forgot_AskingAgainRightAway_DoesNotSendASecondEmail()
    {
        var email = await RegisterAsync();

        await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email });
        var second = await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email });

        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ResetEmailsTo(email).ShouldBe(1);
    }

    [Fact]
    public async Task Reset_SetsTheNewPasswordEndsEverySessionAndWarnsTheUser()
    {
        var email = await RegisterAsync();
        var refreshToken = await LoginAndReadRefreshTokenAsync(email, Password);
        var token = await RequestTokenAsync(email);

        var reset = await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = NewPassword });

        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        reset.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith($"{RefreshTokenCookie.Name}=;", StringComparison.Ordinal));

        (await LoginAsync(email, Password)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginAsync(email, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refresh.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        (await _anonymous.SendAsync(refresh)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await _emails.WaitForAsync(email, "cambió");
    }

    [Fact]
    public async Task Reset_TheSameTokenTwice_OnlyWorksOnce()
    {
        var email = await RegisterAsync();
        var token = await RequestTokenAsync(email);
        await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = NewPassword });

        var again = await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "Third-Passw0rd!" });

        again.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(again)).ShouldBe("Users.InvalidPasswordResetToken");
        (await LoginAsync(email, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_UnknownToken_ReturnsBadRequest()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/auth/reset-password", new { token = "not-a-real-token", newPassword = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(response)).ShouldBe("Users.InvalidPasswordResetToken");
    }

    [Fact]
    public async Task Reset_ShortPassword_ReturnsBadRequestAndKeepsTheToken()
    {
        var email = await RegisterAsync();
        var token = await RequestTokenAsync(email);

        var tooShort = await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "short" });
        var valid = await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = NewPassword });

        tooShort.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        valid.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangingThePassword_SpendsPendingLinksAndWarnsTheUser()
    {
        var email = await RegisterAsync();
        var token = await RequestTokenAsync(email);

        var login = await LoginAsync(email, Password);
        var accessToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        var change = await client.PostAsJsonAsync("/me/password", new { currentPassword = Password, newPassword = NewPassword });
        change.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _emails.WaitForAsync(email, "cambió");

        var reset = await _anonymous.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "Third-Passw0rd!" });
        reset.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var register = await _anonymous.PostAsJsonAsync("/auth/register", new { email, displayName = "Test user", password = Password });
        register.StatusCode.ShouldBe(HttpStatusCode.OK);
        return email;
    }

    // Signing up also sends a welcome email, from the outbox and at its own pace.
    private int ResetEmailsTo(string email) => _emails.To(email).Count(m => m.Subject.Contains("Restablece"));

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _anonymous.PostAsJsonAsync("/auth/login", new { email, password });

    private async Task<string> LoginAndReadRefreshTokenAsync(string email, string password)
    {
        var login = await LoginAsync(email, password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{RefreshTokenCookie.Name}=", StringComparison.Ordinal));
        return cookie.Split(';')[0][(RefreshTokenCookie.Name.Length + 1)..];
    }

    private async Task<string> RequestTokenAsync(string email)
    {
        var forgot = await _anonymous.PostAsJsonAsync("/auth/forgot-password", new { email });
        forgot.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var message = await _emails.WaitForAsync(email, "Restablece");
        return Regex.Match(message.TextBody, "/reset-password#token=([A-Za-z0-9_-]+)").Groups[1].Value;
    }
}
