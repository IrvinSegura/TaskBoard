using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class LabelRepository(ApplicationDbContext db) : ILabelRepository
{
    public async Task<Label?> GetByIdAsync(Guid labelId, CancellationToken cancellationToken) =>
        await db.Labels.FindAsync([labelId], cancellationToken);

    public void Add(Label label) => db.Labels.Add(label);

    public void Remove(Label label) => db.Labels.Remove(label);
}
