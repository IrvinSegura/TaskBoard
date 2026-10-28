using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ChatMessageRepository(ApplicationDbContext db) : IChatMessageRepository
{
    public Task<ChatMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken) =>
        db.ChatMessages
            .Include(m => m.Attachments)
            .SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    public void Add(ChatMessage message) => db.ChatMessages.Add(message);
}
