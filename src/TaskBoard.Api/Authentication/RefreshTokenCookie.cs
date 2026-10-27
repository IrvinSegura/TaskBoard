namespace TaskBoard.Api.Authentication;

/// <summary>
/// The refresh token travels only in this cookie, so scripts in the page (an XSS included) can never read it.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "taskboard-refresh-token";

    // Scoped to /auth: the browser sends it to refresh and logout only, not with every API call.
    public const string Path = "/auth";

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token) ? token : null;

    public static void Append(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, token, CreateOptions(expiresAt));

    // The browser only drops the cookie when path and flags match the ones it was set with.
    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(expiresAt: null));

    // SameSite=Strict keeps other sites from making the browser send it (CSRF). Front end and API must
    // therefore share a site: same domain or subdomains of one, which includes localhost on any port.
    private static CookieOptions CreateOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true
    };
}
