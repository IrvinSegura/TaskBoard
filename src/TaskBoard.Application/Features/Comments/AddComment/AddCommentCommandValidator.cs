using FluentValidation;

namespace TaskBoard.Application.Features.Comments.AddComment;

internal sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
