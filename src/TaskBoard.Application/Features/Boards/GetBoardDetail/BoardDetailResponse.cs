using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Boards.GetBoardDetail;

public sealed record BoardDetailResponse(
    Guid Id,
    string Name,
    BoardVisibility Visibility,
    IReadOnlyList<BoardMemberResponse> Members,
    IReadOnlyList<LabelResponse> Labels,
    IReadOnlyList<ColumnResponse> Columns);

public sealed record BoardMemberResponse(Guid UserId, string DisplayName, BoardRole Role);

public sealed record LabelResponse(Guid Id, string Name, LabelColor Color);

public sealed record ColumnResponse(Guid Id, string Name, double Position, IReadOnlyList<CardSummaryResponse> Cards);

public sealed record CardSummaryResponse(
    Guid Id,
    string Title,
    CardPriority Priority,
    DateTimeOffset? DueDate,
    double Position,
    IReadOnlyList<Guid> AssigneeIds,
    IReadOnlyList<Guid> LabelIds,
    ChecklistProgressResponse? Checklist,
    int CommentCount,
    int AttachmentCount);

// Null when the card has no checklist.
public sealed record ChecklistProgressResponse(int Done, int Total);
