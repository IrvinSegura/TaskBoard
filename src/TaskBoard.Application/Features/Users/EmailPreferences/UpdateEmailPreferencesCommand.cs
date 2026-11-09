using FluentValidation;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Users.EmailPreferences;

public sealed record UpdateEmailPreferencesCommand(IReadOnlyList<NotificationType> MutedTypes) : ICommand;

internal sealed class UpdateEmailPreferencesCommandValidator : AbstractValidator<UpdateEmailPreferencesCommand>
{
    public UpdateEmailPreferencesCommandValidator()
    {
        RuleFor(x => x.MutedTypes).NotNull();

        // JSON also binds plain numbers to an enum, including ones that do not exist.
        RuleForEach(x => x.MutedTypes).IsInEnum();
    }
}

internal sealed class UpdateEmailPreferencesCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateEmailPreferencesCommand>
{
    public async Task<Result> Handle(UpdateEmailPreferencesCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(currentUser.UserId);
        }

        user.MuteEmailNotifications(command.MutedTypes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
