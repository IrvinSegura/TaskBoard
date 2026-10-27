using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Boards.AddBoardMember;
using TaskBoard.Application.Features.Boards.ArchiveBoard;
using TaskBoard.Application.Features.Boards.CreateBoard;
using TaskBoard.Application.Features.Boards.DeleteBoard;
using TaskBoard.Application.Features.Boards.GetBoardDetail;
using TaskBoard.Application.Features.Boards.GetTeamBoards;
using TaskBoard.Application.Features.Boards.RemoveBoardMember;
using TaskBoard.Application.Features.Boards.RenameBoard;
using TaskBoard.Application.Features.Boards.RestoreBoard;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Api.Endpoints;

internal sealed class BoardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Boards").RequireAuthorization();

        group.MapGet("/teams/{teamId:guid}/boards", async (
            Guid teamId,
            bool? archived,
            IQueryHandler<GetTeamBoardsQuery, IReadOnlyList<BoardSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetTeamBoardsQuery(teamId, archived ?? false), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/teams/{teamId:guid}/boards", async (
            Guid teamId,
            CreateBoardRequest request,
            ICommandHandler<CreateBoardCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new CreateBoardCommand(teamId, request.Name, request.Visibility), cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/boards/{id}", new { id }));
        });

        group.MapGet("/boards/{boardId:guid}", async (
            Guid boardId,
            IQueryHandler<GetBoardDetailQuery, BoardDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetBoardDetailQuery(boardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/boards/{boardId:guid}/members", async (
            Guid boardId,
            AddBoardMemberRequest request,
            ICommandHandler<AddBoardMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new AddBoardMemberCommand(boardId, request.UserId, request.Role), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPatch("/boards/{boardId:guid}", async (
            Guid boardId,
            RenameBoardRequest request,
            ICommandHandler<RenameBoardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RenameBoardCommand(boardId, request.Name), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/boards/{boardId:guid}/archive", async (
            Guid boardId,
            ICommandHandler<ArchiveBoardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new ArchiveBoardCommand(boardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/boards/{boardId:guid}/restore", async (
            Guid boardId,
            ICommandHandler<RestoreBoardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RestoreBoardCommand(boardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/boards/{boardId:guid}", async (
            Guid boardId,
            ICommandHandler<DeleteBoardCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteBoardCommand(boardId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/boards/{boardId:guid}/members/{userId:guid}", async (
            Guid boardId,
            Guid userId,
            ICommandHandler<RemoveBoardMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RemoveBoardMemberCommand(boardId, userId), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record CreateBoardRequest(string Name, BoardVisibility Visibility);

    internal sealed record RenameBoardRequest(string Name);

    internal sealed record AddBoardMemberRequest(Guid UserId, BoardRole Role);
}
