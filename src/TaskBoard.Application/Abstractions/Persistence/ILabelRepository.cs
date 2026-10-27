using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface ILabelRepository
{
    Task<Label?> GetByIdAsync(Guid labelId, CancellationToken cancellationToken);

    void Add(Label label);

    // The database removes the label from every card that had it.
    void Remove(Label label);
}
