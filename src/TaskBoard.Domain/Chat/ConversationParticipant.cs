using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

public sealed class ConversationParticipant : Entity
{
    private ConversationParticipant()
    {
    }

    internal ConversationParticipant(Guid conversationId, Guid userId)
    {
        ConversationId = conversationId;
        UserId = userId;
    }

    public Guid ConversationId { get; private set; }

    public Guid UserId { get; private set; }
}
