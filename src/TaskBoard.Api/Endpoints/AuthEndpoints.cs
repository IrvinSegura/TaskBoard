using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth.Login;
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
        });

        group.MapPost("/login", async (
            LoginCommand command,
            ICommandHandler<LoginCommand, AccessTokenResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });
    }
}
