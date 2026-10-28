using FluentValidation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat.RenameConversation;

internal sealed class RenameConversationCommandValidator : AbstractValidator<RenameConversationCommand>
{
    public RenameConversationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(ChatRules.MaxTitleLength);
    }
}
