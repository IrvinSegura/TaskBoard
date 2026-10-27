using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Cards.GetCardDetail;

// Comments are not included; they are paged separately through GET /cards/{cardId}/comments.
public sealed record CardDetailResponse(
    Guid Id,
    Guid BoardId,
    Guid ColumnId,
    string Title,
    string? Description,
    CardPriority Priority,
    DateTimeOffset? DueDate,
    double Position,
    bool IsArchived,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    int CommentCount,
    IReadOnlyList<CardAssigneeResponse> Assignees,
    IReadOnlyList<CardAttachmentResponse> Attachments,
    IReadOnlyList<Guid> LabelIds,
    IReadOnlyList<ChecklistItemResponse> Checklist);

public sealed record CardAssigneeResponse(Guid UserId, string DisplayName);

public sealed record ChecklistItemResponse(Guid Id, string Text, bool IsDone, double Position);

public sealed record CardAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeInBytes,
    Guid UploadedById,
    DateTimeOffset CreatedAt);
