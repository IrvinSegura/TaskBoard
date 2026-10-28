using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Favorites.SetTeamFavorite;

// Stars or unstars a team the current user belongs to. Setting the state it already has is a no-op.
public sealed record SetTeamFavoriteCommand(Guid TeamId, bool IsFavorite) : ICommand;
