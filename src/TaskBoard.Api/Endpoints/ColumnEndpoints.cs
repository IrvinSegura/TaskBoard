using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Columns.CreateColumn;
using TaskBoard.Application.Features.Columns.RenameColumn;

namespace TaskBoard.Api.Endpoints;

internal sealed class ColumnEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Columns").RequireAuthorization();

        group.MapPost("/boards/{boardId:guid}/columns", async (
            Guid boardId,
            CreateColumnRequest request,
            ICommandHandler<CreateColumnCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new CreateColumnCommand(boardId, request.Name), cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/columns/{id}", new { id }));
        });

        group.MapPatch("/columns/{columnId:guid}", async (
            Guid columnId,
            RenameColumnRequest request,
            ICommandHandler<RenameColumnCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RenameColumnCommand(columnId, request.Name), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record CreateColumnRequest(string Name);

    internal sealed record RenameColumnRequest(string Name);
}
