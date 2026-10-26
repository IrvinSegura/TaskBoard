using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Teams.AddTeamMember;
using TaskBoard.Application.Features.Teams.CreateTeam;
using TaskBoard.Application.Features.Teams.GetMyTeams;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Api.Endpoints;

internal sealed class TeamEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams").WithTags("Teams").RequireAuthorization();

        group.MapGet("/", async (
            IQueryHandler<GetMyTeamsQuery, IReadOnlyList<TeamSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyTeamsQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/", async (
            CreateTeamCommand command,
            ICommandHandler<CreateTeamCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/teams/{id}", new { id }));
        });

        group.MapPost("/{teamId:guid}/members", async (
            Guid teamId,
            AddTeamMemberRequest request,
            ICommandHandler<AddTeamMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new AddTeamMemberCommand(teamId, request.UserId, request.Role), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record AddTeamMemberRequest(Guid UserId, TeamRole Role);
}
