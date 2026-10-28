using FluentValidation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat.AddConversationParticipants;

internal sealed class AddConversationParticipantsCommandValidator : AbstractValidator<AddConversationParticipantsCommand>
{
    public AddConversationParticipantsCommandValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty().Must(ids => ids.Count <= ChatRules.MaxGroupParticipants)
            .WithMessage($"A group can have at most {ChatRules.MaxGroupParticipants} people.");
    }
}
