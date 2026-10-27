using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Checklists;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ChecklistItemRepository(ApplicationDbContext db) : IChecklistItemRepository
{
    public async Task<ChecklistItem?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken) =>
        await db.ChecklistItems.FindAsync([itemId], cancellationToken);

    public Task<double?> GetLastPositionAsync(Guid cardId, CancellationToken cancellationToken) =>
        db.ChecklistItems
            .Where(i => i.CardId == cardId)
            .MaxAsync(i => (double?)i.Position, cancellationToken);

    public void Add(ChecklistItem item) => db.ChecklistItems.Add(item);

    public void Remove(ChecklistItem item) => db.ChecklistItems.Remove(item);
}
