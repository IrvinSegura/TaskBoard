using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Behaviors;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        ILogger<CommandHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> inner,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken) =>
            RunAsync(logger, typeof(TQuery).Name, () => inner.Handle(query, cancellationToken));
    }

    private static async Task<TResult> RunAsync<TResult>(ILogger logger, string requestName, Func<Task<TResult>> handle)
        where TResult : Result
    {
        var startedAt = Stopwatch.GetTimestamp();
        var result = await handle();
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (result.IsSuccess)
        {
            logger.LogInformation("{Request} completed in {ElapsedMs:0} ms", requestName, elapsedMs);
        }
        else
        {
            logger.LogWarning(
                "{Request} failed with {ErrorCode} in {ElapsedMs:0} ms", requestName, result.Error.Code, elapsedMs);
        }

        return result;
    }
}
