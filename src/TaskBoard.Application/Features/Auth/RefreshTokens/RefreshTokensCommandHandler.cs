using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.RefreshTokens;

internal sealed class RefreshTokensCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenProvider tokenProvider,
    AuthTokenService tokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RefreshTokensCommandHandler> logger)
    : ICommandHandler<RefreshTokensCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        var stored = await refreshTokens.GetByHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            return UserErrors.InvalidRefreshToken;
        }

        // A token that was already rotated is being used again, so it most likely leaked:
        // end every session of the user instead of letting either party continue.
        if (stored.IsRevoked)
        {
            logger.LogWarning("Revoked refresh token reused for user {UserId}; revoking all sessions", stored.UserId);
            await tokenService.RevokeAllAsync(stored.UserId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return UserErrors.InvalidRefreshToken;
        }

        var now = timeProvider.GetUtcNow();
        if (!stored.IsActive(now))
        {
            return UserErrors.InvalidRefreshToken;
        }

        var user = await users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.InvalidRefreshToken;
        }

        stored.Revoke(now);
        var tokens = tokenService.Issue(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A parallel request rotated the same token first; only one of them gets new tokens.
            return UserErrors.InvalidRefreshToken;
        }

        return tokens;
    }
}
