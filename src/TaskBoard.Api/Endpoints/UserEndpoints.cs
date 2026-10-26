using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth.ChangePassword;
using TaskBoard.Application.Features.Users.GetCurrentUser;

namespace TaskBoard.Api.Endpoints;

internal sealed class UserEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me").WithTags("Users").RequireAuthorization();

        group.MapGet("/", async (
            IQueryHandler<GetCurrentUserQuery, CurrentUserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetCurrentUserQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/password", async (
            ChangePasswordCommand command,
            ICommandHandler<ChangePasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });
    }
}
