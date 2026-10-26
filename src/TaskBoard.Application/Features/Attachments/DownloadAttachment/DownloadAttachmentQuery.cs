using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Attachments.DownloadAttachment;

public sealed record DownloadAttachmentQuery(Guid AttachmentId) : IQuery<AttachmentFileResponse>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Attachment;

    Guid IBoardScopedRequest.ScopeId => AttachmentId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}

// The caller owns Content and must dispose it (Results.File does).
public sealed record AttachmentFileResponse(Stream Content, string ContentType, string FileName);
