using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.SendChatMessage;

// Text, files, or both. Links are plain text; the client turns them into links.
public sealed record SendChatMessageCommand(Guid ConversationId, string? Body, IReadOnlyList<ChatUpload> Files)
    : ICommand<ChatMessageResponse>;

// The content type is not taken from the client; it is derived from the extension.
public sealed record ChatUpload(string FileName, long SizeInBytes, Stream Content);
