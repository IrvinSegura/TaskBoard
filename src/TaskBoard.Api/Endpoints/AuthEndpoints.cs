using TaskBoard.Api.Authentication;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.RateLimiting;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth;
using TaskBoard.Application.Features.Auth.Login;
using TaskBoard.Application.Features.Auth.Logout;
using TaskBoard.Application.Features.Auth.RefreshTokens;
using TaskBoard.Application.Features.Auth.Register;
using TaskBoard.Domain.Users;

namespace TaskBoard.Api.Endpoints;

internal sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (
            RegisterCommand command,
            ICommandHandler<RegisterCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Ok(new { id }));
        })
        .RequireRateLimiting(AuthRateLimitOptions.PolicyName);

        group.MapPost("/login", async (
            LoginCommand command,
            ICommandHandler<LoginCommand, AuthTokensResponse> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(tokens => IssueTokens(httpContext.Response, tokens));
        })
        .RequireRateLimiting(AuthRateLimitOptions.PolicyName);

        // Not rate limited: refresh tokens are 256 random bits, so guessing one is not feasible.
        group.MapPost("/refresh", async (
            ICommandHandler<RefreshTokensCommand, AuthTokensResponse> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = RefreshTokenCookie.Read(httpContext.Request);
            if (refreshToken is null)
            {
                return UserErrors.InvalidRefreshToken.ToProblem();
            }

            var result = await handler.Handle(new RefreshTokensCommand(refreshToken), cancellationToken);
            if (result.IsFailure)
            {
                // A spent or revoked token never works again; drop it so the browser stops sending it.
                RefreshTokenCookie.Delete(httpContext.Response);
            }

            return result.ToHttpResult(tokens => IssueTokens(httpContext.Response, tokens));
        });

        // Anonymous: holding the refresh token is what proves the session, and the access token may have expired.
        group.MapPost("/logout", async (
            ICommandHandler<LogoutCommand> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = RefreshTokenCookie.Read(httpContext.Request);
            RefreshTokenCookie.Delete(httpContext.Response);

            // Without a cookie there is nothing to revoke; logging out stays idempotent.
            if (refreshToken is null)
            {
                return Results.NoContent();
            }

            var result = await handler.Handle(new LogoutCommand(refreshToken), cancellationToken);
            return result.ToHttpResult();
        });
    }

    // The refresh token goes into the cookie only; the body carries what the page itself needs.
    private static IResult IssueTokens(HttpResponse response, AuthTokensResponse tokens)
    {
        RefreshTokenCookie.Append(response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        return Results.Ok(new AccessTokenResponse(tokens.AccessToken, tokens.ExpiresAt));
    }

    internal sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
}
