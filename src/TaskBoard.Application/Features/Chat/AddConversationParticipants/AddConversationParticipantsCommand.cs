using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.AddConversationParticipants;

// Groups only. They see the whole history once they are in.
public sealed record AddConversationParticipantsCommand(Guid ConversationId, IReadOnlyList<Guid> UserIds) : ICommand;
