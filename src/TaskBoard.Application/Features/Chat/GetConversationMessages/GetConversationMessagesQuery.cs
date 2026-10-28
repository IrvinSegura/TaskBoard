using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Chat.GetConversationMessages;

/// <summary>
/// The newest messages, or the ones right before <paramref name="Before"/> when loading older history.
/// Each page is returned oldest first, ready to be shown top to bottom.
/// </summary>
public sealed record GetConversationMessagesQuery(Guid ConversationId, Guid? Before, int? Limit)
    : IQuery<ChatMessagesPageResponse>
{
    public const int DefaultLimit = 50;

    public const int MaxLimit = 100;
}
