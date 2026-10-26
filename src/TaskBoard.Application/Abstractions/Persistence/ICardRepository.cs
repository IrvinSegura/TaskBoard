using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface ICardRepository
{
    // Loads the card with its assignments.
    Task<Card?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken);

    Task<double?> GetLastPositionAsync(Guid columnId, CancellationToken cancellationToken);

    // Position of a card only if it lives in the given column; used to place a moved card between neighbours.
    Task<double?> GetPositionInColumnAsync(Guid cardId, Guid columnId, CancellationToken cancellationToken);

    void Add(Card card);
}
