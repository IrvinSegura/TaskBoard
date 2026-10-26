using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Cards.SearchCards;

/// <summary>
/// Filters a board's cards; every filter is optional and they combine with AND.
/// <paramref name="Archived"/> switches between active cards (default) and archived ones.
/// </summary>
public sealed record SearchCardsQuery(
    Guid BoardId,
    string? Text,
    Guid? ColumnId,
    Guid? AssigneeId,
    CardPriority? Priority,
    DateTimeOffset? DueBefore,
    bool Archived)
    : IQuery<IReadOnlyList<CardSearchResultResponse>>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}

public sealed record CardSearchResultResponse(
    Guid Id,
    Guid ColumnId,
    string Title,
    CardPriority Priority,
    DateTimeOffset? DueDate,
    bool IsArchived,
    IReadOnlyList<Guid> AssigneeIds);
