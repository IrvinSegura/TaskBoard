using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.DeleteChatMessage;

// Only the sender. The message stays in the history as deleted; its text and files are gone.
public sealed record DeleteChatMessageCommand(Guid MessageId) : ICommand;
