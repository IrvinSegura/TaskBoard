using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Application.Common.Files;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.SendChatMessage;

internal sealed class SendChatMessageCommandHandler(
    ChatAccess access,
    IChatMessageRepository messages,
    IReadDbContext db,
    IFileStorage fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<SendChatMessageCommandHandler> logger)
    : ICommandHandler<SendChatMessageCommand, ChatMessageResponse>
{
    public async Task<Result<ChatMessageResponse>> Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
    {
        if (!await access.CanOpenAsync(command.ConversationId, currentUser.UserId, cancellationToken))
        {
            return ChatErrors.NotFound(command.ConversationId);
        }

        // Check every file before storing any, so a bad one does not leave the others behind.
        var checkedFiles = new List<(ChatUpload Upload, string FileName, string ContentType)>();
        foreach (var upload in command.Files)
        {
            // Some browsers send a full client path; keep only the name.
            var fileName = Path.GetFileName(upload.FileName);
            var extension = Path.GetExtension(fileName);

            if (!ChatRules.TryGetContentType(extension, out var contentType))
            {
                return ChatErrors.TypeNotAllowed;
            }

            if (!await FileSignatures.MatchesAsync(upload.Content, extension, cancellationToken))
            {
                return ChatErrors.ContentMismatch;
            }

            checkedFiles.Add((upload, fileName, contentType));
        }

        var stored = new List<ChatFile>();
        Result<ChatMessage> sent;
        try
        {
            foreach (var (upload, fileName, contentType) in checkedFiles)
            {
                var storageKey = await fileStorage.UploadAsync(upload.Content, contentType, cancellationToken);
                stored.Add(new ChatFile(fileName, contentType, upload.SizeInBytes, storageKey));
            }

            sent = ChatMessage.Send(command.ConversationId, currentUser.UserId, command.Body, stored);
            if (sent.IsFailure)
            {
                await TryDeleteFilesAsync(stored);
                return sent.Error;
            }

            messages.Add(sent.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // No row points to these files, so remove them now. If even this fails (or the process dies before
            // reaching it), the orphaned file cleanup job deletes them later.
            await TryDeleteFilesAsync(stored);
            throw;
        }

        return (await ChatQueries.GetMessageAsync(db, sent.Value.Id, cancellationToken))!;
    }

    private async Task TryDeleteFilesAsync(IEnumerable<ChatFile> files)
    {
        foreach (var file in files)
        {
            try
            {
                await fileStorage.DeleteAsync(file.StorageKey, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete file {StorageKey} after its message failed to save", file.StorageKey);
            }
        }
    }
}
