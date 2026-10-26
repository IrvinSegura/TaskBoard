using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Boards.GetBoardDetail;

public sealed record BoardDetailResponse(
    Guid Id,
    string Name,
    BoardVisibility Visibility,
    IReadOnlyList<BoardMemberResponse> Members,
    IReadOnlyList<ColumnResponse> Columns);

public sealed record BoardMemberResponse(Guid UserId, string DisplayName, BoardRole Role);

public sealed record ColumnResponse(Guid Id, string Name, double Position, IReadOnlyList<CardSummaryResponse> Cards);

public sealed record CardSummaryResponse(
    Guid Id,
    string Title,
    CardPriority Priority,
    DateTimeOffset? DueDate,
    double Position,
    IReadOnlyList<Guid> AssigneeIds);
