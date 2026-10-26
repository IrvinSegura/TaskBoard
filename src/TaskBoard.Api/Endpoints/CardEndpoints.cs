using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Cards.AssignCardMember;
using TaskBoard.Application.Features.Cards.CreateCard;
using TaskBoard.Application.Features.Cards.MoveCard;

namespace TaskBoard.Api.Endpoints;

internal sealed class CardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Cards").RequireAuthorization();

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
    }

    internal sealed record CreateCardRequest(string Title, string? Description);

    internal sealed record MoveCardRequest(Guid TargetColumnId, Guid? PreviousCardId, Guid? NextCardId);

    internal sealed record AssignCardMemberRequest(Guid UserId);
}
