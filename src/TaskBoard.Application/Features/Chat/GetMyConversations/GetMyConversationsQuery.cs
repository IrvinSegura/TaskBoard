using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.GetMyConversations;

// Every conversation the current user can open, except the chats of archived boards.
public sealed record GetMyConversationsQuery : IQuery<IReadOnlyList<ConversationResponse>>;
