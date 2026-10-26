using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Auth.Logout;

internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenProvider tokenProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand>
{
    // Also succeeds for unknown or already revoked tokens: logging out twice is not an error,
    // and the answer does not reveal whether a token exists.
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        var stored = await refreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (stored is { IsRevoked: false })
        {
            stored.Revoke(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
