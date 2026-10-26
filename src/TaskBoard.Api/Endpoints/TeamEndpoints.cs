using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Teams.AddTeamMember;
using TaskBoard.Application.Features.Teams.ChangeTeamMemberRole;
using TaskBoard.Application.Features.Teams.CreateTeam;
using TaskBoard.Application.Features.Teams.GetMyTeams;
using TaskBoard.Application.Features.Teams.RemoveTeamMember;
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

        group.MapPatch("/{teamId:guid}/members/{userId:guid}", async (
            Guid teamId,
            Guid userId,
            ChangeTeamMemberRoleRequest request,
            ICommandHandler<ChangeTeamMemberRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new ChangeTeamMemberRoleCommand(teamId, userId, request.Role), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/{teamId:guid}/members/{userId:guid}", async (
            Guid teamId,
            Guid userId,
            ICommandHandler<RemoveTeamMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RemoveTeamMemberCommand(teamId, userId), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record AddTeamMemberRequest(Guid UserId, TeamRole Role);

    internal sealed record ChangeTeamMemberRoleRequest(TeamRole Role);
}
