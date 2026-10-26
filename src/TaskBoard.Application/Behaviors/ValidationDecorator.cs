using FluentValidation;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Behaviors;

internal static class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(command, validators, cancellationToken);
            return error is null ? await inner.Handle(command, cancellationToken) : Result.Failure(error);
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(command, validators, cancellationToken);
            return error is null ? await inner.Handle(command, cancellationToken) : Result.Failure<TResponse>(error);
        }
    }

    private static async Task<ValidationError?> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        var validatorList = validators.ToArray();
        if (validatorList.Length == 0)
        {
            return null;
        }

        var context = new ValidationContext<TCommand>(command);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToArray();

        return errors.Length == 0 ? null : new ValidationError(errors);
    }
}
