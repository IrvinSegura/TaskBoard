using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(ApplicationDbContext db) : ICardRepository
{
    public Task<Card?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken) =>
        db.Cards
            .Include(c => c.Assignments)
            .SingleOrDefaultAsync(c => c.Id == cardId, cancellationToken);

    public Task<double?> GetLastPositionAsync(Guid columnId, CancellationToken cancellationToken) =>
        db.Cards
            .Where(c => c.ColumnId == columnId)
            .MaxAsync(c => (double?)c.Position, cancellationToken);

    public Task<double?> GetPositionInColumnAsync(Guid cardId, Guid columnId, CancellationToken cancellationToken) =>
        db.Cards
            .Where(c => c.Id == cardId && c.ColumnId == columnId)
            .Select(c => (double?)c.Position)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Card>> GetColumnCardsAsync(Guid columnId, CancellationToken cancellationToken) =>
        await db.Cards
            .Where(c => c.ColumnId == columnId)
            .OrderBy(c => c.Position)
            .ToListAsync(cancellationToken);

    public void Add(Card card) => db.Cards.Add(card);

    public void Remove(Card card) => db.Cards.Remove(card);
}
