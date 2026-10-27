using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Activities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ActivityRepository(ApplicationDbContext db) : IActivityRepository
{
    public void Add(ActivityEntry entry) => db.Activities.Add(entry);
}
