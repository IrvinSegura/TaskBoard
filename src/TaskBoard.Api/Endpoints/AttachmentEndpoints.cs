using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Attachments.DeleteAttachment;
using TaskBoard.Application.Features.Attachments.DownloadAttachment;
using TaskBoard.Application.Features.Attachments.UploadAttachment;

namespace TaskBoard.Api.Endpoints;

internal sealed class AttachmentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Attachments").RequireAuthorization();

        group.MapPost("/cards/{cardId:guid}/attachments", async (
            Guid cardId,
            IFormFile file,
            ICommandHandler<UploadAttachmentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            await using var content = file.OpenReadStream();
            var command = new UploadAttachmentCommand(cardId, file.FileName, file.Length, content);

            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/attachments/{id}", new { id }));
        })
        // Antiforgery protects cookie-authenticated forms; this API authenticates with bearer tokens.
        .DisableAntiforgery();

        // Never served as static files: every download goes through the board membership check.
        group.MapGet("/attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            IQueryHandler<DownloadAttachmentQuery, AttachmentFileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DownloadAttachmentQuery(attachmentId), cancellationToken);

            // Sent as a download (Content-Disposition: attachment) so the browser never renders it inline.
            return result.ToHttpResult(file => Results.File(file.Content, file.ContentType, file.FileName));
        });

        group.MapDelete("/attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            ICommandHandler<DeleteAttachmentCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteAttachmentCommand(attachmentId), cancellationToken);
            return result.ToHttpResult();
        });
    }
}
