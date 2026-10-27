using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Cards.AddCardLabel;
using TaskBoard.Application.Features.Cards.RemoveCardLabel;
using TaskBoard.Application.Features.Labels.CreateLabel;
using TaskBoard.Application.Features.Labels.DeleteLabel;
using TaskBoard.Application.Features.Labels.UpdateLabel;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Api.Endpoints;

internal sealed class LabelEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Labels").RequireAuthorization();

        group.MapPost("/boards/{boardId:guid}/labels", async (
            Guid boardId,
            LabelRequest request,
            ICommandHandler<CreateLabelCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new CreateLabelCommand(boardId, request.Name, request.Color), cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/boards/{boardId}/labels/{id}", new { id }));
        });

        group.MapPut("/boards/{boardId:guid}/labels/{labelId:guid}", async (
            Guid boardId,
            Guid labelId,
            LabelRequest request,
            ICommandHandler<UpdateLabelCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new UpdateLabelCommand(boardId, labelId, request.Name, request.Color), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/boards/{boardId:guid}/labels/{labelId:guid}", async (
            Guid boardId,
            Guid labelId,
            ICommandHandler<DeleteLabelCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteLabelCommand(boardId, labelId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/cards/{cardId:guid}/labels", async (
            Guid cardId,
            AddCardLabelRequest request,
            ICommandHandler<AddCardLabelCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new AddCardLabelCommand(cardId, request.LabelId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/cards/{cardId:guid}/labels/{labelId:guid}", async (
            Guid cardId,
            Guid labelId,
            ICommandHandler<RemoveCardLabelCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RemoveCardLabelCommand(cardId, labelId), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record LabelRequest(string Name, LabelColor Color);

    internal sealed record AddCardLabelRequest(Guid LabelId);
}
