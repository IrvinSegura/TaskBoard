using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface ITeamRepository
{
    // Loads the team with its members.
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);

    void Add(Team team);

    // The database deletes the team's boards, and everything in them, with it.
    void Remove(Team team);
}
