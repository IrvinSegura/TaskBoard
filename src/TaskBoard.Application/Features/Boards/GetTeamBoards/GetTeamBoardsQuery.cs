using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.GetTeamBoards;

public sealed record GetTeamBoardsQuery(Guid TeamId) : IQuery<IReadOnlyList<BoardSummaryResponse>>;

public sealed record BoardSummaryResponse(Guid Id, string Name, BoardVisibility Visibility);
