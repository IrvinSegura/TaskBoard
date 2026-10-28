using FluentValidation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat.CreateGroupConversation;

internal sealed class CreateGroupConversationCommandValidator : AbstractValidator<CreateGroupConversationCommand>
{
    public CreateGroupConversationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(ChatRules.MaxTitleLength);
        RuleFor(x => x.ParticipantIds).NotEmpty().Must(ids => ids.Count <= ChatRules.MaxGroupParticipants)
            .WithMessage($"A group can have at most {ChatRules.MaxGroupParticipants} people.");
    }
}
