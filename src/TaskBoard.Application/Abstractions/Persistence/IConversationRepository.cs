using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IConversationRepository
{
    // Loads the conversation with its participants.
    Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<Conversation?> GetDirectAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken);

    void Add(Conversation conversation);

    Task<ConversationReadMarker?> GetReadMarkerAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);

    void AddReadMarker(ConversationReadMarker marker);
}
