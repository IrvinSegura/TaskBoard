using FluentValidation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat.EditChatMessage;

internal sealed class EditChatMessageCommandValidator : AbstractValidator<EditChatMessageCommand>
{
    public EditChatMessageCommandValidator()
    {
        RuleFor(x => x.Body).MaximumLength(ChatRules.MaxBodyLength);
    }
}
