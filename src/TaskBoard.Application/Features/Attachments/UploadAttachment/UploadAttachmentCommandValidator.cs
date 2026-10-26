using FluentValidation;
using TaskBoard.Domain.Attachments;

namespace TaskBoard.Application.Features.Attachments.UploadAttachment;

internal sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(AttachmentRules.MaxFileNameLength)
            .Must(name => AttachmentRules.IsAllowedExtension(Path.GetExtension(name)))
            .WithMessage("This file type is not allowed.");

        RuleFor(x => x.SizeInBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(AttachmentRules.MaxSizeInBytes);
    }
}
