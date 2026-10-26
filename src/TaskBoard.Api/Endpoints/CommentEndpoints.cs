using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Comments.AddComment;
using TaskBoard.Application.Features.Comments.GetCardComments;

namespace TaskBoard.Api.Endpoints;

internal sealed class CommentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cards/{cardId:guid}/comments").WithTags("Comments").RequireAuthorization();

        group.MapGet("/", async (
            Guid cardId,
            IQueryHandler<GetCardCommentsQuery, IReadOnlyList<CommentResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetCardCommentsQuery(cardId), cancellationToken);
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
    }

    internal sealed record AddCommentRequest(string Body);
}
