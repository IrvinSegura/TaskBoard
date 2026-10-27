using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface ICardRepository
{
    // Loads the card with its assignments and labels.
    Task<Card?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken);

    Task<double?> GetLastPositionAsync(Guid columnId, CancellationToken cancellationToken);

    // Position of a card only if it lives in the given column; used to place a moved card between neighbours.
    Task<double?> GetPositionInColumnAsync(Guid cardId, Guid columnId, CancellationToken cancellationToken);

    // Every card of the column, archived ones included, ordered by position. Used to renumber the column.
    Task<IReadOnlyList<Card>> GetColumnCardsAsync(Guid columnId, CancellationToken cancellationToken);

    void Add(Card card);

    // The database deletes the card's comments and attachments with it.
    void Remove(Card card);
}
