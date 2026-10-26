using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Attachments.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid AttachmentId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Attachment;

    Guid IBoardScopedRequest.ScopeId => AttachmentId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
