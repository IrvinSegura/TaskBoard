using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.EditChatMessage;

// Only the sender, and only the text; the files of the message stay as they are.
public sealed record EditChatMessageCommand(Guid MessageId, string? Body) : ICommand;
