using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Comments.GetCardComments;

internal sealed class GetCardCommentsQueryHandler(IReadDbContext db)
    : IQueryHandler<GetCardCommentsQuery, PagedResponse<CommentResponse>>
{
    public async Task<Result<PagedResponse<CommentResponse>>> Handle(
        GetCardCommentsQuery query,
        CancellationToken cancellationToken)
    {
        var comments = await (
            from comment in db.Comments
            join author in db.Users on comment.AuthorId equals author.Id
            where comment.CardId == query.CardId
            orderby comment.CreatedAt, comment.Id
            select new CommentResponse(
                comment.Id,
                author.Id,
                author.DisplayName,
                comment.Body,
                comment.IsEdited,
                comment.CreatedAt))
            .ToPagedResponseAsync(query.Page, cancellationToken);

        return comments;
    }
}
