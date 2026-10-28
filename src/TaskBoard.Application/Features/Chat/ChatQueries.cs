using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat;

// Projections shared by the chat queries, the send handler and the realtime events.
internal static class ChatQueries
{
    public const int PreviewLength = 140;

    // Unordered: a join after OrderBy does not keep the order, so callers sort the results.
    public static IQueryable<ChatMessageResponse> ToResponses(this IQueryable<ChatMessage> messages, IReadDbContext db) =>
        from message in messages
        join sender in db.Users on message.SenderId equals sender.Id
        select new ChatMessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderId,
            sender.DisplayName,
            message.Body,
            message.IsEdited,
            message.IsDeleted,
            message.CreatedAt,
            message.Attachments
                .OrderBy(a => a.Position)
                .Select(a => new ChatAttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeInBytes))
                .ToList());

    public static Task<ChatMessageResponse?> GetMessageAsync(
        IReadDbContext db,
        Guid messageId,
        CancellationToken cancellationToken) =>
        db.ChatMessages.Where(m => m.Id == messageId).ToResponses(db).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// The conversations as <paramref name="userId"/> sees them, most recently active first.
    /// </summary>
    public static async Task<List<ConversationResponse>> ToResponsesAsync(
        this IQueryable<Conversation> conversations,
        IReadDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from conversation in conversations
            let readAt = db.ConversationReadMarkers
                .Where(r => r.ConversationId == conversation.Id && r.UserId == userId)
                .Select(r => (DateTimeOffset?)r.LastReadAt)
                .FirstOrDefault()
            let board = db.Boards
                .Where(b => b.Id == conversation.BoardId)
                .Select(b => new { b.Name, b.TeamId })
                .FirstOrDefault()
            select new
            {
                conversation.Id,
                conversation.Kind,
                conversation.Title,
                conversation.BoardId,
                conversation.TeamId,
                conversation.CreatedAt,
                BoardName = board == null ? null : board.Name,
                BoardTeamId = board == null ? (Guid?)null : board.TeamId,
                TeamName = db.Teams
                    .Where(t => t.Id == conversation.TeamId || (board != null && t.Id == board.TeamId))
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                LastMessage = db.ChatMessages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .ThenByDescending(m => m.Id)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        SenderName = db.Users.Where(u => u.Id == m.SenderId).Select(u => u.DisplayName).FirstOrDefault(),
                        m.Body,
                        AttachmentCount = m.Attachments.Count(),
                        m.IsDeleted,
                        m.CreatedAt
                    })
                    .FirstOrDefault(),
                UnreadCount = db.ChatMessages.Count(m =>
                    m.ConversationId == conversation.Id &&
                    m.SenderId != userId &&
                    !m.IsDeleted &&
                    (readAt == null || m.CreatedAt > readAt))
            })
            .ToListAsync(cancellationToken);

        var withParticipants = rows
            .Where(r => r.Kind is ConversationKind.Direct or ConversationKind.Group)
            .Select(r => r.Id)
            .ToList();

        var participants = (await (
                from participant in db.ConversationParticipants
                join user in db.Users on participant.UserId equals user.Id
                where withParticipants.Contains(participant.ConversationId)
                select new { participant.ConversationId, User = new ChatUserResponse(user.Id, user.DisplayName, user.Email) })
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.ConversationId, p => p.User);

        return rows
            .Select(row =>
            {
                var people = participants[row.Id].OrderBy(u => u.DisplayName).ToList();
                var title = row.Kind switch
                {
                    ConversationKind.Group => row.Title ?? string.Empty,
                    ConversationKind.Board => row.BoardName ?? string.Empty,
                    ConversationKind.Team => row.TeamName ?? string.Empty,
                    _ => (people.FirstOrDefault(u => u.Id != userId) ?? people.FirstOrDefault())?.DisplayName ?? string.Empty
                };
                var lastMessage = row.LastMessage is { } last
                    ? new LastMessageResponse(
                        last.Id,
                        last.SenderId,
                        last.SenderName ?? string.Empty,
                        Preview(last.Body),
                        last.AttachmentCount,
                        last.IsDeleted,
                        last.CreatedAt)
                    : null;

                return new ConversationResponse(
                    row.Id,
                    row.Kind,
                    title,
                    row.BoardId,
                    row.TeamId ?? row.BoardTeamId,
                    row.TeamName,
                    people,
                    lastMessage,
                    row.UnreadCount,
                    row.CreatedAt);
            })
            .OrderByDescending(c => c.LastMessage?.CreatedAt ?? c.CreatedAt)
            .ToList();
    }

    private static string? Preview(string? body) =>
        body is { Length: > PreviewLength } ? string.Concat(body.AsSpan(0, PreviewLength - 1), "…") : body;
}
