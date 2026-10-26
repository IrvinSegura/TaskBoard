using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Attachments.UploadAttachment;

internal sealed class UploadAttachmentCommandHandler(
    ICardRepository cards,
    IAttachmentRepository attachments,
    IFileStorage fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UploadAttachmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        // Some browsers send a full client path; keep only the name.
        var fileName = Path.GetFileName(command.FileName);
        var extension = Path.GetExtension(fileName);

        if (!AttachmentRules.TryGetContentType(extension, out var contentType))
        {
            return AttachmentErrors.TypeNotAllowed;
        }

        if (!await FileSignatures.MatchesAsync(command.Content, extension, cancellationToken))
        {
            return AttachmentErrors.ContentMismatch;
        }

        // TODO: if SaveChanges fails the uploaded file is orphaned; add a cleanup job.
        var storageKey = await fileStorage.UploadAsync(command.Content, contentType, cancellationToken);

        var attachment = Attachment.Create(
            card.Id,
            card.BoardId,
            currentUser.UserId,
            fileName,
            contentType,
            command.SizeInBytes,
            storageKey);

        attachments.Add(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return attachment.Id;
    }
}
