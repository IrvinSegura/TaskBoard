using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Behaviors;

/// <summary>
/// Single enforcement point for "only board members": every request implementing
/// <see cref="IBoardScopedRequest"/> is checked here before its handler runs.
/// </summary>
internal static class BoardAuthorizationDecorator
{
    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> inner, BoardAuthorizer authorizer)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var error = await authorizer.AuthorizeAsync(command, cancellationToken);
            return error is null ? await inner.Handle(command, cancellationToken) : Result.Failure(error);
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        BoardAuthorizer authorizer)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var error = await authorizer.AuthorizeAsync(command, cancellationToken);
            return error is null ? await inner.Handle(command, cancellationToken) : Result.Failure<TResponse>(error);
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> inner, BoardAuthorizer authorizer)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var error = await authorizer.AuthorizeAsync(query, cancellationToken);
            return error is null ? await inner.Handle(query, cancellationToken) : Result.Failure<TResponse>(error);
        }
    }
}

internal sealed class BoardAuthorizer(IBoardAccessService boardAccess, ICurrentUser currentUser)
{
    public async Task<Error?> AuthorizeAsync(object request, CancellationToken cancellationToken)
    {
        if (request is not IBoardScopedRequest scoped)
        {
            return null;
        }

        var boardId = await boardAccess.ResolveBoardIdAsync(scoped.Scope, scoped.ScopeId, cancellationToken);
        if (boardId is null)
        {
            return AuthorizationErrors.ResourceNotFound;
        }

        var role = await boardAccess.GetRoleAsync(boardId.Value, currentUser.UserId, cancellationToken);
        if (role is null)
        {
            return AuthorizationErrors.ResourceNotFound;
        }

        return role.Value < scoped.MinimumRole ? AuthorizationErrors.InsufficientRole : null;
    }
}
