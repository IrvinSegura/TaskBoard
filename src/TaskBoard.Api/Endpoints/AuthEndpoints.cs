using TaskBoard.Api.Extensions;
using TaskBoard.Api.RateLimiting;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth;
using TaskBoard.Application.Features.Auth.Login;
using TaskBoard.Application.Features.Auth.Logout;
using TaskBoard.Application.Features.Auth.RefreshTokens;
using TaskBoard.Application.Features.Auth.Register;

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
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireRateLimiting(AuthRateLimitOptions.PolicyName);

        // Not rate limited: refresh tokens are 256 random bits, so guessing one is not feasible.
        group.MapPost("/refresh", async (
            RefreshTokensCommand command,
            ICommandHandler<RefreshTokensCommand, AuthTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        // Anonymous: holding the refresh token is what proves the session, and the access token may have expired.
        group.MapPost("/logout", async (
            LogoutCommand command,
            ICommandHandler<LogoutCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });
    }
}
