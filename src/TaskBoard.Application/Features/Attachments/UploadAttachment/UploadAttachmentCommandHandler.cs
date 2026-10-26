using Microsoft.Extensions.Logging;
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
    IUnitOfWork unitOfWork,
    ILogger<UploadAttachmentCommandHandler> logger)
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

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // No row points to the file, so remove it now. If even this fails (or the process dies before
            // reaching it), the orphaned file cleanup job deletes it later.
            await TryDeleteFileAsync(storageKey);
            throw;
        }

        return attachment.Id;
    }

    private async Task TryDeleteFileAsync(string storageKey)
    {
        try
        {
            await fileStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete file {StorageKey} after its attachment failed to save", storageKey);
        }
    }
}
