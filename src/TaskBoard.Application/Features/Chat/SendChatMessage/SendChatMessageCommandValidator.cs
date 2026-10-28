using FluentValidation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Application.Features.Chat.SendChatMessage;

internal sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Body).MaximumLength(ChatRules.MaxBodyLength);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Body) || x.Files.Count > 0)
            .WithName("body")
            .WithMessage("A message needs text or at least one file.");

        RuleFor(x => x.Files)
            .Must(files => files.Count <= ChatRules.MaxAttachmentsPerMessage)
            .WithMessage($"A message can have at most {ChatRules.MaxAttachmentsPerMessage} files.")
            .Must(files => files.Sum(f => f.SizeInBytes) <= ChatRules.MaxMessageSizeInBytes)
            .WithMessage("The files of this message are too large together.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.FileName)
                .NotEmpty()
                .MaximumLength(ChatRules.MaxFileNameLength)
                .Must(name => ChatRules.IsAllowedExtension(Path.GetExtension(name)))
                .WithMessage("This file type is not allowed.");

            file.RuleFor(f => f.SizeInBytes).GreaterThan(0).LessThanOrEqualTo(ChatRules.MaxFileSizeInBytes);
        });
    }
}
