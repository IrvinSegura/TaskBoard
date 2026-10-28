using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.GetChatContacts;

// The people the current user shares a team or a board with, to start conversations with.
public sealed record GetChatContactsQuery : IQuery<IReadOnlyList<ChatUserResponse>>;
