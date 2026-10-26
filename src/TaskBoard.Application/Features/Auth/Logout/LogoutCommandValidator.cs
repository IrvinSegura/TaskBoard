using FluentValidation;

namespace TaskBoard.Application.Features.Auth.Logout;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
