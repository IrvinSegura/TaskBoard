using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.GetTeamBoards;

// Archived false lists active boards; true lists archived ones so they can be restored.
public sealed record GetTeamBoardsQuery(Guid TeamId, bool Archived = false)
    : IQuery<IReadOnlyList<BoardSummaryResponse>>;

public sealed record BoardSummaryResponse(Guid Id, string Name, BoardVisibility Visibility);
