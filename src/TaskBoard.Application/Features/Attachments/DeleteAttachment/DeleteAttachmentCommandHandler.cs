using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Attachments.DeleteAttachment;

internal sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachments,
    IBoardAccessService boardAccess,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand command, CancellationToken cancellationToken)
    {
        var attachment = await attachments.GetByIdAsync(command.AttachmentId, cancellationToken);
        if (attachment is null)
        {
            return AttachmentErrors.NotFound(command.AttachmentId);
        }

        var role = await boardAccess.GetRoleAsync(attachment.BoardId, currentUser.UserId, cancellationToken);

        var result = attachment.Delete(currentUser.UserId, role == BoardRole.Admin);
        if (result.IsFailure)
        {
            return result;
        }

        // The stored file is deleted by AttachmentDeletedEventHandler, only after this row is gone.
        attachments.Remove(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
