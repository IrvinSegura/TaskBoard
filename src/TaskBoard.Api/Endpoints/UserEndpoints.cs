using TaskBoard.Api.Authentication;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.RateLimiting;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Auth.ChangePassword;
using TaskBoard.Application.Features.Cards.GetMyCards;
using TaskBoard.Application.Features.Users.EmailPreferences;
using TaskBoard.Application.Features.Users.GetCurrentUser;
using TaskBoard.Application.Features.Users.SendVerificationEmail;
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

        group.MapGet("/cards", async (
            IQueryHandler<GetMyCardsQuery, IReadOnlyList<MyCardResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyCardsQuery(), cancellationToken);
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

        group.MapGet("/email-preferences", async (
            IQueryHandler<GetEmailPreferencesQuery, EmailPreferencesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetEmailPreferencesQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/email-preferences", async (
            UpdateEmailPreferencesCommand command,
            ICommandHandler<UpdateEmailPreferencesCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        // Rate limited for the email it sends.
        group.MapPost("/verification-email", async (
            ICommandHandler<SendVerificationEmailCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new SendVerificationEmailCommand(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireRateLimiting(AuthRateLimitOptions.PolicyName);

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
