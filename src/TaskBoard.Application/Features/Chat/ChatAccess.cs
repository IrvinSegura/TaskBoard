using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat;

/// <summary>
/// Who can open a conversation: the participants of a direct or group conversation, everyone who can open
/// the board of a board conversation, and the members of the team of a team conversation.
/// Membership is read when it is needed, so losing access to a board or team also closes its chat.
/// </summary>
internal sealed class ChatAccess(IReadDbContext db)
{
    public IQueryable<Conversation> VisibleTo(Guid userId) =>
        db.Conversations.Where(c =>
            ((c.Kind == ConversationKind.Direct || c.Kind == ConversationKind.Group) &&
                db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.UserId == userId)) ||
            (c.Kind == ConversationKind.Board && db.Boards.Any(b =>
                b.Id == c.BoardId &&
                (b.Members.Any(m => m.UserId == userId) ||
                    (b.Visibility == BoardVisibility.Team &&
                        db.TeamMembers.Any(m => m.TeamId == b.TeamId && m.UserId == userId))))) ||
            (c.Kind == ConversationKind.Team &&
                db.TeamMembers.Any(m => m.TeamId == c.TeamId && m.UserId == userId)));

    public Task<bool> CanOpenAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken) =>
        VisibleTo(userId).AnyAsync(c => c.Id == conversationId, cancellationToken);

    // Everyone who can open the conversation right now: who its realtime events go to.
    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => new { c.Kind, c.BoardId, c.TeamId })
            .SingleOrDefaultAsync(cancellationToken);

        switch (conversation?.Kind)
        {
            case ConversationKind.Direct or ConversationKind.Group:
                return await db.ConversationParticipants
                    .Where(p => p.ConversationId == conversationId)
                    .Select(p => p.UserId)
                    .ToListAsync(cancellationToken);

            case ConversationKind.Board:
                var board = await db.Boards
                    .Where(b => b.Id == conversation.BoardId)
                    .Select(b => new { b.TeamId, b.Visibility, MemberIds = b.Members.Select(m => m.UserId).ToList() })
                    .SingleOrDefaultAsync(cancellationToken);
                if (board is null)
                {
                    return [];
                }

                if (board.Visibility != BoardVisibility.Team)
                {
                    return board.MemberIds;
                }

                var teamMemberIds = await TeamMemberIdsAsync(board.TeamId, cancellationToken);
                return [.. board.MemberIds.Union(teamMemberIds)];

            case ConversationKind.Team:
                return await TeamMemberIdsAsync(conversation.TeamId!.Value, cancellationToken);

            default:
                return [];
        }
    }

    private Task<List<Guid>> TeamMemberIdsAsync(Guid teamId, CancellationToken cancellationToken) =>
        db.TeamMembers.Where(m => m.TeamId == teamId).Select(m => m.UserId).ToListAsync(cancellationToken);
}
