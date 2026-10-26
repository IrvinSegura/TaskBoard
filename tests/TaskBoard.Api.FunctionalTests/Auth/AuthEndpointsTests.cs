using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Api.Authentication;

namespace TaskBoard.Api.FunctionalTests.Auth;

public class AuthEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private const string Password = "Passw0rd!Passw0rd";

    // Cookies are sent and read by hand, so every test states exactly which refresh token it uses.
    private readonly HttpClient _anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = await RegisterAsync();

        var login = await _anonymous.PostAsJsonAsync("/auth/login", new { email, password = "Wrong-password-1" });

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ApiDriver.ReadErrorCodeAsync(login)).ShouldBe("Users.InvalidCredentials");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        var login = await _anonymous.PostAsJsonAsync(
            "/auth/login", new { email = $"nobody-{Guid.NewGuid():N}@example.com", password = Password });

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ApiDriver.ReadErrorCodeAsync(login)).ShouldBe("Users.InvalidCredentials");
    }

    [Fact]
    public async Task Register_WithEmailAlreadyTaken_ReturnsConflict()
    {
        var email = await RegisterAsync();

        // Emails are compared after normalizing, so case and surrounding spaces do not make a new account.
        var duplicate = await _anonymous.PostAsJsonAsync(
            "/auth/register", new { email = $"  {email.ToUpperInvariant()} ", displayName = "Copy", password = Password });

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ApiDriver.ReadErrorCodeAsync(duplicate)).ShouldBe("Users.EmailTaken");
    }

    [Fact]
    public async Task ExpiredAccessToken_IsRejected()
    {
        var email = await RegisterAsync();
        var tokens = await LoginAsync(email);
        var userId = (await GetMeAsync(tokens.AccessToken)).GetProperty("id").GetGuid();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateExpiredToken(userId));
        var response = await client.GetAsync("/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsTheSignedInUser()
    {
        var email = await RegisterAsync();
        var tokens = await LoginAsync(email);

        var me = await GetMeAsync(tokens.AccessToken);

        me.GetProperty("email").GetString().ShouldBe(email);
        me.GetProperty("displayName").GetString().ShouldBe("Test user");
    }

    [Fact]
    public async Task Refresh_ReturnsNewTokensAndRotatesTheRefreshToken()
    {
        var tokens = await LoginAsync(await RegisterAsync());

        var refresh = await RefreshAsync(tokens.RefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewed = await ReadTokensAsync(refresh);

        renewed.RefreshToken.ShouldNotBe(tokens.RefreshToken);
        (await GetMeAsync(renewed.AccessToken)).GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);

        // The renewed token works once more; the original one is spent.
        (await RefreshAsync(renewed.RefreshToken))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReusingARotatedToken_RevokesEverySessionOfTheUser()
    {
        var email = await RegisterAsync();
        var laptop = await LoginAsync(email);
        var phone = await LoginAsync(email);
        var renewedLaptop = await ReadTokensAsync(
            await RefreshAsync(laptop.RefreshToken));

        var reuse = await RefreshAsync(laptop.RefreshToken);

        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ApiDriver.ReadErrorCodeAsync(reuse)).ShouldBe("Users.InvalidRefreshToken");
        (await RefreshAsync(renewedLaptop.RefreshToken))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await RefreshAsync(phone.RefreshToken))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorizedAndClearsTheCookie()
    {
        var refresh = await RefreshAsync("not-a-real-token");

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldClearRefreshCookie(refresh);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var refresh = await _anonymous.PostAsync("/auth/refresh", null);

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ApiDriver.ReadErrorCodeAsync(refresh)).ShouldBe("Users.InvalidRefreshToken");
    }

    [Fact]
    public async Task Login_PutsTheRefreshTokenOnlyInAnHttpOnlyCookieScopedToAuth()
    {
        var email = await RegisterAsync();

        var login = await _anonymous.PostAsJsonAsync("/auth/login", new { email, password = Password });

        var cookie = GetRefreshCookie(login).ToLowerInvariant();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/auth");
        cookie.ShouldContain("expires=");

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("refreshToken", out _).ShouldBeFalse();
        body.GetProperty("expiresAt").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshTokenAndCanBeRepeated()
    {
        var tokens = await LoginAsync(await RegisterAsync());

        var logout = await LogoutAsync(tokens.RefreshToken);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ShouldClearRefreshCookie(logout);
        (await LogoutAsync(tokens.RefreshToken))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _anonymous.PostAsync("/auth/logout", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refresh = await RefreshAsync(tokens.RefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_IsRejected()
    {
        var tokens = await LoginAsync(await RegisterAsync());
        var client = CreateClient(tokens.AccessToken);

        var change = await client.PostAsJsonAsync(
            "/me/password", new { currentPassword = "Wrong-password-1", newPassword = "N3w-Passw0rd!" });

        change.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(change)).ShouldBe("Users.WrongPassword");
    }

    [Fact]
    public async Task ChangePassword_SwitchesPasswordsAndEndsOtherSessions()
    {
        var email = await RegisterAsync();
        var tokens = await LoginAsync(email);
        const string newPassword = "N3w-Passw0rd!";

        var change = await CreateClient(tokens.AccessToken).PostAsJsonAsync(
            "/me/password", new { currentPassword = Password, newPassword });
        change.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ShouldClearRefreshCookie(change);

        (await RefreshAsync(tokens.RefreshToken))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _anonymous.PostAsJsonAsync("/auth/login", new { email, password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _anonymous.PostAsJsonAsync("/auth/login", new { email, password = newPassword }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_TooManyAttempts_IsRateLimited()
    {
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3"));
        var client = limited.CreateClient();
        var attempt = new { email = "brute-force@example.com", password = "Guess-123456" };

        for (var i = 0; i < 3; i++)
        {
            (await client.PostAsJsonAsync("/auth/login", attempt)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var blocked = await client.PostAsJsonAsync("/auth/login", attempt);

        blocked.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        blocked.Headers.RetryAfter.ShouldNotBeNull();
        (await ApiDriver.ReadErrorCodeAsync(blocked)).ShouldBe("RateLimit.Exceeded");
    }

    [Fact]
    public async Task Login_BehindATrustedProxy_IsRateLimitedPerForwardedClient()
    {
        await using var proxied = CreateProxiedFactory(trustProxy: true);
        var client = proxied.CreateClient();

        (await LoginFromAsync(client, "203.0.113.1")).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginFromAsync(client, "203.0.113.1")).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginFromAsync(client, "203.0.113.1")).ShouldBe(HttpStatusCode.TooManyRequests);

        // Another user behind the same proxy has a limit of their own.
        (await LoginFromAsync(client, "203.0.113.2")).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_FromAnUntrustedAddress_IgnoresForwardedHeaders()
    {
        await using var proxied = CreateProxiedFactory(trustProxy: false);
        var client = proxied.CreateClient();

        // Changing X-Forwarded-For on every attempt does not get around the limit.
        (await LoginFromAsync(client, "203.0.113.1")).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginFromAsync(client, "203.0.113.2")).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginFromAsync(client, "203.0.113.3")).ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void Startup_WithoutSigningKey_FailsWithAClearMessage()
    {
        using var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SigningKey", ""));

        var exception = Should.Throw<OptionsValidationException>(() => misconfigured.CreateClient());

        exception.Message.ShouldContain("Jwt__SigningKey");
    }

    // Every request reaches the API from 10.0.0.1, as if it went through a reverse proxy at that address.
    private WebApplicationFactory<Program> CreateProxiedFactory(bool trustProxy) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "2");
            if (trustProxy)
            {
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", ProxyAddress.ToString());
            }

            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ProxyAddressStartupFilter()));
        });

    private static async Task<HttpStatusCode> LoginFromAsync(HttpClient client, string clientAddress)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new { email = "proxied@example.com", password = "Guess-123456" })
        };
        request.Headers.Add("X-Forwarded-For", clientAddress);

        return (await client.SendAsync(request)).StatusCode;
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var register = await _anonymous.PostAsJsonAsync("/auth/register", new { email, displayName = "Test user", password = Password });
        register.StatusCode.ShouldBe(HttpStatusCode.OK);
        return email;
    }

    private async Task<Tokens> LoginAsync(string email)
    {
        var login = await _anonymous.PostAsJsonAsync("/auth/login", new { email, password = Password });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadTokensAsync(login);
    }

    private async Task<JsonElement> GetMeAsync(string accessToken)
    {
        var me = await CreateClient(accessToken).GetAsync("/me");
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await me.Content.ReadFromJsonAsync<JsonElement>();
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<Tokens> ReadTokensAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new Tokens(body.GetProperty("accessToken").GetString()!, ReadCookieValue(GetRefreshCookie(response)));
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        SendWithRefreshCookieAsync("/auth/refresh", refreshToken);

    private Task<HttpResponseMessage> LogoutAsync(string refreshToken) =>
        SendWithRefreshCookieAsync("/auth/logout", refreshToken);

    private async Task<HttpResponseMessage> SendWithRefreshCookieAsync(string path, string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        return await _anonymous.SendAsync(request);
    }

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        return cookies!.Single(c => c.StartsWith($"{RefreshTokenCookie.Name}=", StringComparison.Ordinal));
    }

    private static string ReadCookieValue(string setCookie) =>
        setCookie.Split(';')[0][(RefreshTokenCookie.Name.Length + 1)..];

    // A deleted cookie comes back empty and already expired, on the same path it was set on.
    private static void ShouldClearRefreshCookie(HttpResponseMessage response)
    {
        var cookie = GetRefreshCookie(response);
        ReadCookieValue(cookie).ShouldBeEmpty();
        cookie.ToLowerInvariant().ShouldContain("expires=thu, 01 jan 1970");
        cookie.ToLowerInvariant().ShouldContain("path=/auth");
    }

    // Signed with the API's key and claims, so expiry is the only thing wrong with it.
    private static string CreateExpiredToken(Guid userId)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]),
            Issuer = "taskboard-api",
            Audience = "taskboard-client",
            IssuedAt = now.AddHours(-2),
            NotBefore = now.AddHours(-2),
            Expires = now.AddHours(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TaskBoardApiFactory.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static readonly IPAddress ProxyAddress = IPAddress.Parse("10.0.0.1");

    private sealed record Tokens(string AccessToken, string RefreshToken);

    // Runs before the API's own middleware, so forwarded headers see the request as coming from the proxy.
    private sealed class ProxyAddressStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ProxyAddress;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
