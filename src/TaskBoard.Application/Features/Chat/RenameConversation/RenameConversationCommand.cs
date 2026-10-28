using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.RenameConversation;

// Groups only; any participant can rename them.
public sealed record RenameConversationCommand(Guid ConversationId, string Title) : ICommand;
