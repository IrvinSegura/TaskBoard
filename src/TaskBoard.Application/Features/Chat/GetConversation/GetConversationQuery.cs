using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.GetConversation;

public sealed record GetConversationQuery(Guid ConversationId) : IQuery<ConversationResponse>;
