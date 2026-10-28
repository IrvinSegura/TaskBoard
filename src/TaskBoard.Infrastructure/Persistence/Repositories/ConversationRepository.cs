using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ConversationRepository(ApplicationDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken) =>
        db.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

    public Task<Conversation?> GetDirectAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken)
    {
        var directKey = ChatRules.DirectKey(userId, otherUserId);
        return db.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c => c.DirectKey == directKey, cancellationToken);
    }

    public void Add(Conversation conversation) => db.Conversations.Add(conversation);

    public Task<ConversationReadMarker?> GetReadMarkerAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        db.ConversationReadMarkers.SingleOrDefaultAsync(
            m => m.ConversationId == conversationId && m.UserId == userId,
            cancellationToken);

    public void AddReadMarker(ConversationReadMarker marker) => db.ConversationReadMarkers.Add(marker);
}
