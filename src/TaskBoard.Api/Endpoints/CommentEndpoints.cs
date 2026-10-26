using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Application.Features.Comments.AddComment;
using TaskBoard.Application.Features.Comments.DeleteComment;
using TaskBoard.Application.Features.Comments.EditComment;
using TaskBoard.Application.Features.Comments.GetCardComments;

namespace TaskBoard.Api.Endpoints;

internal sealed class CommentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cards/{cardId:guid}/comments").WithTags("Comments").RequireAuthorization();

        group.MapGet("/", async (
            Guid cardId,
            int? page,
            int? pageSize,
            IQueryHandler<GetCardCommentsQuery, PagedResponse<CommentResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCardCommentsQuery(cardId, PageRequest.Create(page, pageSize));
            var result = await handler.Handle(query, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/", async (
            Guid cardId,
            AddCommentRequest request,
            ICommandHandler<AddCommentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new AddCommentCommand(cardId, request.Body), cancellationToken);
            return result.ToHttpResult(id => Results.Ok(new { id }));
        });

        app.MapPatch("/comments/{commentId:guid}", async (
            Guid commentId,
            EditCommentRequest request,
            ICommandHandler<EditCommentCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new EditCommentCommand(commentId, request.Body), cancellationToken);
            return result.ToHttpResult();
        })
        .WithTags("Comments")
        .RequireAuthorization();

        app.MapDelete("/comments/{commentId:guid}", async (
            Guid commentId,
            ICommandHandler<DeleteCommentCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteCommentCommand(commentId), cancellationToken);
            return result.ToHttpResult();
        })
        .WithTags("Comments")
        .RequireAuthorization();
    }

    internal sealed record AddCommentRequest(string Body);

    internal sealed record EditCommentRequest(string Body);
}
