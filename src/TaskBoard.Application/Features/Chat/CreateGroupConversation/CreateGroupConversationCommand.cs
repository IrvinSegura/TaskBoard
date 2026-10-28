using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.CreateGroupConversation;

// The creator joins the group too; listing them in ParticipantIds is fine.
public sealed record CreateGroupConversationCommand(string Title, IReadOnlyList<Guid> ParticipantIds) : ICommand<Guid>;
