using TaskBoard.Domain.Activities;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IActivityRepository
{
    void Add(ActivityEntry entry);
}
