using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat;

public sealed record ChatUserResponse(Guid Id, string DisplayName, string Email);

public sealed record ChatAttachmentResponse(Guid Id, string FileName, string ContentType, long SizeInBytes);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string? Body,
    bool IsEdited,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ChatAttachmentResponse> Attachments);

// The newest message, for the conversation list. Body is cut to a preview.
public sealed record LastMessageResponse(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string? Body,
    int AttachmentCount,
    bool IsDeleted,
    DateTimeOffset CreatedAt);

/// <summary>
/// A conversation as the current user sees it. Title is the group name, the board or team name, or the other
/// person's name in a direct conversation. Participants is empty for board and team conversations.
/// </summary>
public sealed record ConversationResponse(
    Guid Id,
    ConversationKind Kind,
    string Title,
    Guid? BoardId,
    Guid? TeamId,
    string? TeamName,
    IReadOnlyList<ChatUserResponse> Participants,
    LastMessageResponse? LastMessage,
    int UnreadCount,
    DateTimeOffset CreatedAt);

public sealed record ChatMessagesPageResponse(IReadOnlyList<ChatMessageResponse> Items, bool HasMore);
