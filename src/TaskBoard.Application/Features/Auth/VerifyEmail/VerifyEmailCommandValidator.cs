using FluentValidation;

namespace TaskBoard.Application.Features.Auth.VerifyEmail;

internal sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
    }
}
