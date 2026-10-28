using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Attachments.DownloadAttachment;

namespace TaskBoard.Application.Features.Chat.DownloadChatAttachment;

public sealed record DownloadChatAttachmentQuery(Guid AttachmentId) : IQuery<AttachmentFileResponse>;
