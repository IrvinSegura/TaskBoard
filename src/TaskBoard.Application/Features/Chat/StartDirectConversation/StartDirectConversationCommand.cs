using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.StartDirectConversation;

// Opens the direct conversation with someone, by id or by email, creating it the first time.
public sealed record StartDirectConversationCommand(Guid? UserId, string? Email) : ICommand<Guid>;
