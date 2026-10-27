using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Application.Features.Cards.ArchiveCard;
using TaskBoard.Application.Features.Cards.AssignCardMember;
using TaskBoard.Application.Features.Cards.CreateCard;
using TaskBoard.Application.Features.Cards.DeleteCard;
using TaskBoard.Application.Features.Cards.GetCardDetail;
using TaskBoard.Application.Features.Cards.MoveCard;
using TaskBoard.Application.Features.Cards.RestoreCard;
using TaskBoard.Application.Features.Cards.SearchCards;
using TaskBoard.Application.Features.Cards.UnassignCardMember;
using TaskBoard.Application.Features.Cards.UpdateCard;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Api.Endpoints;

internal sealed class CardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Cards").RequireAuthorization();

        group.MapGet("/cards/{cardId:guid}", async (
            Guid cardId,
            IQueryHandler<GetCardDetailQuery, CardDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetCardDetailQuery(cardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/boards/{boardId:guid}/cards", async (
            Guid boardId,
            [AsParameters] SearchCardsRequest request,
            IQueryHandler<SearchCardsQuery, PagedResponse<CardSearchResultResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchCardsQuery(
                boardId,
                request.Text,
                request.ColumnId,
                request.AssigneeId,
                request.Priority,
                request.DueBefore,
                request.Archived ?? false,
                PageRequest.Create(request.Page, request.PageSize));
            var result = await handler.Handle(query, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/columns/{columnId:guid}/cards", async (
            Guid columnId,
            CreateCardRequest request,
            ICommandHandler<CreateCardCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new CreateCardCommand(columnId, request.Title, request.Description), cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/cards/{id}", new { id }));
        });

        group.MapPatch("/cards/{cardId:guid}/move", async (
            Guid cardId,
            MoveCardRequest request,
            ICommandHandler<MoveCardCommand, CardMovedResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new MoveCardCommand(cardId, request.TargetColumnId, request.PreviousCardId, request.NextCardId);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/cards/{cardId:guid}/assignees", async (
            Guid cardId,
            AssignCardMemberRequest request,
            ICommandHandler<AssignCardMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new AssignCardMemberCommand(cardId, request.UserId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/cards/{cardId:guid}", async (
            Guid cardId,
            UpdateCardRequest request,
            ICommandHandler<UpdateCardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCardCommand(
                cardId, request.Title, request.Description, request.Priority, request.DueDate);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/cards/{cardId:guid}/archive", async (
            Guid cardId,
            ICommandHandler<ArchiveCardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new ArchiveCardCommand(cardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/cards/{cardId:guid}/restore", async (
            Guid cardId,
            ICommandHandler<RestoreCardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RestoreCardCommand(cardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/cards/{cardId:guid}", async (
            Guid cardId,
            ICommandHandler<DeleteCardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteCardCommand(cardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/cards/{cardId:guid}/assignees/{userId:guid}", async (
            Guid cardId,
            Guid userId,
            ICommandHandler<UnassignCardMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new UnassignCardMemberCommand(cardId, userId), cancellationToken);
            return result.ToHttpResult();
        });
    }

    // Bound from the query string, e.g. /boards/{id}/cards?text=login&priority=High&archived=true&page=2.
    internal sealed record SearchCardsRequest(
        string? Text,
        Guid? ColumnId,
        Guid? AssigneeId,
        CardPriority? Priority,
        DateTimeOffset? DueBefore,
        bool? Archived,
        int? Page,
        int? PageSize);

    internal sealed record CreateCardRequest(string Title, string? Description);

    internal sealed record UpdateCardRequest(
        string Title,
        string? Description,
        CardPriority Priority,
        DateTimeOffset? DueDate);

    internal sealed record MoveCardRequest(Guid TargetColumnId, Guid? PreviousCardId, Guid? NextCardId);

    internal sealed record AssignCardMemberRequest(Guid UserId);
}
