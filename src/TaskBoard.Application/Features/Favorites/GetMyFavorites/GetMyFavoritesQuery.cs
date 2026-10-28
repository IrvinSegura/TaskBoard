using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Teams.GetMyTeams;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Favorites.GetMyFavorites;

/// <summary>
/// The teams and boards the current user starred, by name. Stars on teams they left or on boards they can
/// no longer see (or that are archived) stay stored but are not listed, so they come back with the access.
/// </summary>
public sealed record GetMyFavoritesQuery : IQuery<FavoritesResponse>;

public sealed record FavoritesResponse(IReadOnlyList<TeamSummaryResponse> Teams, IReadOnlyList<FavoriteBoardResponse> Boards);

public sealed record FavoriteBoardResponse(Guid Id, string Name, BoardVisibility Visibility, Guid TeamId, string TeamName);
