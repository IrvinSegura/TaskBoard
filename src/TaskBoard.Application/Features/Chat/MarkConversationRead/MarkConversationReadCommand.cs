using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.MarkConversationRead;

// Everything posted so far counts as read by the current user.
public sealed record MarkConversationReadCommand(Guid ConversationId) : ICommand;
