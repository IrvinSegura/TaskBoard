namespace TaskBoard.Application.Features.Cards.MoveCard;

public sealed record CardMovedResponse(Guid CardId, Guid ColumnId, double Position);
