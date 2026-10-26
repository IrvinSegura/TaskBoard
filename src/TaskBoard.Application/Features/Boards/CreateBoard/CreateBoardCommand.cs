using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.CreateBoard;

public sealed record CreateBoardCommand(Guid TeamId, string Name, BoardVisibility Visibility) : ICommand<Guid>;
