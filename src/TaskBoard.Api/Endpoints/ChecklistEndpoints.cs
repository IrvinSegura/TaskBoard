using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Checklists.AddChecklistItem;
using TaskBoard.Application.Features.Checklists.DeleteChecklistItem;
using TaskBoard.Application.Features.Checklists.UpdateChecklistItem;

namespace TaskBoard.Api.Endpoints;

internal sealed class ChecklistEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cards/{cardId:guid}/checklist").WithTags("Checklists").RequireAuthorization();

        group.MapPost("/", async (
            Guid cardId,
            AddChecklistItemRequest request,
            ICommandHandler<AddChecklistItemCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new AddChecklistItemCommand(cardId, request.Text), cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/cards/{cardId}/checklist/{id}", new { id }));
        });

        group.MapPatch("/{itemId:guid}", async (
            Guid cardId,
            Guid itemId,
            UpdateChecklistItemRequest request,
            ICommandHandler<UpdateChecklistItemCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateChecklistItemCommand(cardId, itemId, request.Text, request.IsDone);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/{itemId:guid}", async (
            Guid cardId,
            Guid itemId,
            ICommandHandler<DeleteChecklistItemCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteChecklistItemCommand(cardId, itemId), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record AddChecklistItemRequest(string Text);

    internal sealed record UpdateChecklistItemRequest(string? Text, bool? IsDone);
}
