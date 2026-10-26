using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResponse>;

public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);
