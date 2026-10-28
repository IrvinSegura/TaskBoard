using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IChatMessageRepository
{
    // Loads the message with its attachments.
    Task<ChatMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken);

    void Add(ChatMessage message);
}
