using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.GetConversation;

internal sealed class GetConversationQueryHandler(IReadDbContext db, ChatAccess access, ICurrentUser currentUser)
    : IQueryHandler<GetConversationQuery, ConversationResponse>
{
    public async Task<Result<ConversationResponse>> Handle(GetConversationQuery query, CancellationToken cancellationToken)
    {
        // People who cannot open it get the same answer as for a conversation that does not exist.
        var conversations = await access.VisibleTo(currentUser.UserId)
            .Where(c => c.Id == query.ConversationId)
            .ToResponsesAsync(db, currentUser.UserId, cancellationToken);

        return conversations.Count == 1 ? conversations[0] : ChatErrors.NotFound(query.ConversationId);
    }
}
