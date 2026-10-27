using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class TeamRepository(ApplicationDbContext db) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        db.Teams
            .Include(t => t.Members)
            .SingleOrDefaultAsync(t => t.Id == teamId, cancellationToken);

    public void Add(Team team) => db.Teams.Add(team);

    public void Remove(Team team) => db.Teams.Remove(team);
}
