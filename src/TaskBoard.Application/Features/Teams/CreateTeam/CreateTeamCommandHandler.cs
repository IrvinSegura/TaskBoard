using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.CreateTeam;

internal sealed class CreateTeamCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTeamCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var team = Team.Create(command.Name, currentUser.UserId);
        teams.Add(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.Id;
    }
}
