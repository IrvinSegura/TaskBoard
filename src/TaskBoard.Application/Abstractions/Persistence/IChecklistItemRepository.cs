using TaskBoard.Domain.Checklists;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IChecklistItemRepository
{
    Task<ChecklistItem?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task<double?> GetLastPositionAsync(Guid cardId, CancellationToken cancellationToken);

    void Add(ChecklistItem item);

    void Remove(ChecklistItem item);
}
