using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Comments.GetCardComments;

internal sealed class GetCardCommentsQueryHandler(IReadDbContext db)
    : IQueryHandler<GetCardCommentsQuery, IReadOnlyList<CommentResponse>>
{
    public async Task<Result<IReadOnlyList<CommentResponse>>> Handle(
        GetCardCommentsQuery query,
        CancellationToken cancellationToken)
    {
        // TODO: paginate once threads get long.
        var comments = await (
            from comment in db.Comments
            join author in db.Users on comment.AuthorId equals author.Id
            where comment.CardId == query.CardId
            orderby comment.CreatedAt
            select new CommentResponse(
                comment.Id,
                author.Id,
                author.DisplayName,
                comment.Body,
                comment.IsEdited,
                comment.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CommentResponse>>(comments);
    }
}
