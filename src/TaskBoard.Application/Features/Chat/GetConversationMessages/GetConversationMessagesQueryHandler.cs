using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.GetConversationMessages;

internal sealed class GetConversationMessagesQueryHandler(IReadDbContext db, ChatAccess access, ICurrentUser currentUser)
    : IQueryHandler<GetConversationMessagesQuery, ChatMessagesPageResponse>
{
    public async Task<Result<ChatMessagesPageResponse>> Handle(
        GetConversationMessagesQuery query,
        CancellationToken cancellationToken)
    {
        if (!await access.CanOpenAsync(query.ConversationId, currentUser.UserId, cancellationToken))
        {
            return ChatErrors.NotFound(query.ConversationId);
        }

        var limit = Math.Clamp(query.Limit ?? GetConversationMessagesQuery.DefaultLimit, 1, GetConversationMessagesQuery.MaxLimit);
        var messages = db.ChatMessages.Where(m => m.ConversationId == query.ConversationId);

        // Keyset paging on (CreatedAt, Id): messages arriving meanwhile cannot shift the pages.
        if (query.Before is { } beforeId)
        {
            var cursor = await db.ChatMessages
                .Where(m => m.Id == beforeId && m.ConversationId == query.ConversationId)
                .Select(m => new { m.CreatedAt, m.Id })
                .SingleOrDefaultAsync(cancellationToken);
            if (cursor is null)
            {
                return ChatErrors.MessageNotFound(beforeId);
            }

            messages = messages.Where(m =>
                m.CreatedAt < cursor.CreatedAt || (m.CreatedAt == cursor.CreatedAt && m.Id.CompareTo(cursor.Id) < 0));
        }

        var page = messages
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit + 1);

        var items = (await page.ToResponses(db).ToListAsync(cancellationToken))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToList();

        // One extra row tells whether older messages remain; it is the oldest one, so drop it from the front.
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(0);
        }

        return new ChatMessagesPageResponse(items, hasMore);
    }
}
