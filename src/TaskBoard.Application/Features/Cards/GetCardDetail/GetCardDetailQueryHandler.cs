using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.GetCardDetail;

internal sealed class GetCardDetailQueryHandler(IReadDbContext db) : IQueryHandler<GetCardDetailQuery, CardDetailResponse>
{
    public async Task<Result<CardDetailResponse>> Handle(GetCardDetailQuery query, CancellationToken cancellationToken)
    {
        var card = await db.Cards
            .Where(c => c.Id == query.CardId)
            .Select(c => new
            {
                c.Id,
                c.BoardId,
                c.ColumnId,
                c.Title,
                c.Description,
                c.Priority,
                c.DueDate,
                c.Position,
                c.IsArchived,
                c.CreatedById,
                c.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(query.CardId);
        }

        var assignees = await (
            from assignment in db.Cards.Where(c => c.Id == query.CardId).SelectMany(c => c.Assignments)
            join user in db.Users on assignment.UserId equals user.Id
            orderby user.DisplayName
            select new CardAssigneeResponse(user.Id, user.DisplayName))
            .ToListAsync(cancellationToken);

        var attachments = await db.Attachments
            .Where(a => a.CardId == query.CardId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new CardAttachmentResponse(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeInBytes,
                a.UploadedById,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        var commentCount = await db.Comments.CountAsync(c => c.CardId == query.CardId, cancellationToken);

        return new CardDetailResponse(
            card.Id,
            card.BoardId,
            card.ColumnId,
            card.Title,
            card.Description,
            card.Priority,
            card.DueDate,
            card.Position,
            card.IsArchived,
            card.CreatedById,
            card.CreatedAt,
            commentCount,
            assignees,
            attachments);
    }
}
