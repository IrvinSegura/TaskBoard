using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Favorites.GetMyFavorites;
using TaskBoard.Application.Features.Favorites.SetBoardFavorite;
using TaskBoard.Application.Features.Favorites.SetTeamFavorite;

namespace TaskBoard.Api.Endpoints;

// PUT stars and DELETE unstars; both are idempotent.
internal sealed class FavoriteEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me/favorites").WithTags("Favorites").RequireAuthorization();

        group.MapGet("/", async (
            IQueryHandler<GetMyFavoritesQuery, FavoritesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyFavoritesQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/boards/{boardId:guid}", async (
            Guid boardId,
            ICommandHandler<SetBoardFavoriteCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new SetBoardFavoriteCommand(boardId, IsFavorite: true), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/boards/{boardId:guid}", async (
            Guid boardId,
            ICommandHandler<SetBoardFavoriteCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new SetBoardFavoriteCommand(boardId, IsFavorite: false), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/teams/{teamId:guid}", async (
            Guid teamId,
            ICommandHandler<SetTeamFavoriteCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new SetTeamFavoriteCommand(teamId, IsFavorite: true), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/teams/{teamId:guid}", async (
            Guid teamId,
            ICommandHandler<SetTeamFavoriteCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new SetTeamFavoriteCommand(teamId, IsFavorite: false), cancellationToken);
            return result.ToHttpResult();
        });
    }
}
