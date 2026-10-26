using FluentValidation;

namespace TaskBoard.Application.Features.Auth.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        // Same length rules as registration.
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must be different from the current one.");
    }
}
