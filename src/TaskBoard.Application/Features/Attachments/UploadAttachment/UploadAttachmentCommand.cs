using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Attachments.UploadAttachment;

// The content type is not taken from the client; it is derived from the extension.
public sealed record UploadAttachmentCommand(Guid CardId, string FileName, long SizeInBytes, Stream Content)
    : ICommand<Guid>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
