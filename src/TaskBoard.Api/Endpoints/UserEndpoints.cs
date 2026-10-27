using TaskBoard.Api.Authentication;
using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth.ChangePassword;
using TaskBoard.Application.Features.Users.GetCurrentUser;
using TaskBoard.Application.Features.Users.UpdateProfile;

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

        group.MapPatch("/", async (
            UpdateProfileCommand command,
            ICommandHandler<UpdateProfileCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/password", async (
            ChangePasswordCommand command,
            ICommandHandler<ChangePasswordCommand> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                // Every refresh token of the user was just revoked, this session's one included.
                RefreshTokenCookie.Delete(httpContext.Response);
            }

            return result.ToHttpResult();
        });
    }
}
