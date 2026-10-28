using FluentValidation;

namespace TaskBoard.Application.Features.Chat.StartDirectConversation;

internal sealed class StartDirectConversationCommandValidator : AbstractValidator<StartDirectConversationCommand>
{
    public StartDirectConversationCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue != !string.IsNullOrWhiteSpace(x.Email))
            .WithName("user")
            .WithMessage("Send either a user id or an email.");

        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
