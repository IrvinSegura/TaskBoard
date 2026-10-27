using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Boards.GetBoardDetail;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Cards.GetMyCards;

/// <summary>
/// The active cards assigned to the current user on boards they still belong to and that are not archived,
/// soonest due date first and cards without one last.
/// </summary>
public sealed record GetMyCardsQuery : IQuery<IReadOnlyList<MyCardResponse>>;

public sealed record MyCardResponse(
    Guid Id,
    string Title,
    CardPriority Priority,
    DateTimeOffset? DueDate,
    Guid BoardId,
    string BoardName,
    string ColumnName,
    string TeamName,
    IReadOnlyList<LabelResponse> Labels,
    ChecklistProgressResponse? Checklist);
