using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Attachments.DownloadAttachment;
using TaskBoard.Application.Features.Chat;
using TaskBoard.Application.Features.Chat.AddConversationParticipants;
using TaskBoard.Application.Features.Chat.CreateGroupConversation;
using TaskBoard.Application.Features.Chat.DeleteChatMessage;
using TaskBoard.Application.Features.Chat.DownloadChatAttachment;
using TaskBoard.Application.Features.Chat.EditChatMessage;
using TaskBoard.Application.Features.Chat.GetChatContacts;
using TaskBoard.Application.Features.Chat.GetConversation;
using TaskBoard.Application.Features.Chat.GetConversationMessages;
using TaskBoard.Application.Features.Chat.GetMyConversations;
using TaskBoard.Application.Features.Chat.LeaveConversation;
using TaskBoard.Application.Features.Chat.MarkConversationRead;
using TaskBoard.Application.Features.Chat.RenameConversation;
using TaskBoard.Application.Features.Chat.SendChatMessage;
using TaskBoard.Application.Features.Chat.StartDirectConversation;
using TaskBoard.Domain.Chat;

namespace TaskBoard.Api.Endpoints;

// Conversations nobody can open answer 404, the same as conversations that do not exist.
internal sealed class ChatEndpoints : IEndpoint
{
    // Leave room above the files for the text and the multipart envelope.
    private const long MaxMessageRequestBytes = ChatRules.MaxMessageSizeInBytes + 1024 * 1024;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/chat").WithTags("Chat").RequireAuthorization();

        group.MapGet("/conversations", async (
            IQueryHandler<GetMyConversationsQuery, IReadOnlyList<ConversationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyConversationsQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/contacts", async (
            IQueryHandler<GetChatContactsQuery, IReadOnlyList<ChatUserResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetChatContactsQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/conversations/direct", async (
            StartDirectConversationCommand command,
            ICommandHandler<StartDirectConversationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Ok(new { id }));
        });

        group.MapPost("/conversations/group", async (
            CreateGroupConversationCommand command,
            ICommandHandler<CreateGroupConversationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/chat/conversations/{id}", new { id }));
        });

        group.MapGet("/conversations/{conversationId:guid}", async (
            Guid conversationId,
            IQueryHandler<GetConversationQuery, ConversationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetConversationQuery(conversationId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPatch("/conversations/{conversationId:guid}", async (
            Guid conversationId,
            RenameConversationRequest request,
            ICommandHandler<RenameConversationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RenameConversationCommand(conversationId, request.Title), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/conversations/{conversationId:guid}/participants", async (
            Guid conversationId,
            AddParticipantsRequest request,
            ICommandHandler<AddConversationParticipantsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddConversationParticipantsCommand(conversationId, request.UserIds);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/conversations/{conversationId:guid}/participants/me", async (
            Guid conversationId,
            ICommandHandler<LeaveConversationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new LeaveConversationCommand(conversationId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/conversations/{conversationId:guid}/read", async (
            Guid conversationId,
            ICommandHandler<MarkConversationReadCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new MarkConversationReadCommand(conversationId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            Guid? before,
            int? limit,
            IQueryHandler<GetConversationMessagesQuery, ChatMessagesPageResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetConversationMessagesQuery(conversationId, before, limit);
            var result = await handler.Handle(query, cancellationToken);
            return result.ToHttpResult();
        });

        // Multipart: an optional "body" field and any number of "files".
        group.MapPost("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            [FromForm] string? body,
            IFormFileCollection files,
            ICommandHandler<SendChatMessageCommand, ChatMessageResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var uploads = new List<ChatUpload>(files.Count);
            try
            {
                foreach (var file in files)
                {
                    uploads.Add(new ChatUpload(file.FileName, file.Length, file.OpenReadStream()));
                }

                var command = new SendChatMessageCommand(conversationId, body, uploads);
                var result = await handler.Handle(command, cancellationToken);
                // The message as everyone will see it, so the sender can show it without waiting for the push.
                return result.ToHttpResult();
            }
            finally
            {
                foreach (var upload in uploads)
                {
                    await upload.Content.DisposeAsync();
                }
            }
        })
        .WithMetadata(new RequestSizeLimitAttribute(MaxMessageRequestBytes))
        .WithFormOptions(multipartBodyLengthLimit: MaxMessageRequestBytes)
        // Antiforgery protects cookie-authenticated forms; this API authenticates with bearer tokens.
        .DisableAntiforgery();

        group.MapPatch("/messages/{messageId:guid}", async (
            Guid messageId,
            EditMessageRequest request,
            ICommandHandler<EditChatMessageCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new EditChatMessageCommand(messageId, request.Body), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/messages/{messageId:guid}", async (
            Guid messageId,
            ICommandHandler<DeleteChatMessageCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DeleteChatMessageCommand(messageId), cancellationToken);
            return result.ToHttpResult();
        });

        // Never served as static files: every download goes through the conversation access check.
        group.MapGet("/attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            IQueryHandler<DownloadChatAttachmentQuery, AttachmentFileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DownloadChatAttachmentQuery(attachmentId), cancellationToken);

            // Sent as a download (Content-Disposition: attachment) so the browser never renders it inline.
            return result.ToHttpResult(file => Results.File(file.Content, file.ContentType, file.FileName));
        });
    }

    internal sealed record RenameConversationRequest(string Title);

    internal sealed record AddParticipantsRequest(IReadOnlyList<Guid> UserIds);

    internal sealed record EditMessageRequest(string? Body);
}
