using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.LeaveConversation;

// Groups only: the current user stops taking part. Their messages stay.
public sealed record LeaveConversationCommand(Guid ConversationId) : ICommand;
