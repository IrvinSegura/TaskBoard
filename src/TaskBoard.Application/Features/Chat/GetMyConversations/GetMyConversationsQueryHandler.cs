using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.GetMyConversations;

internal sealed class GetMyConversationsQueryHandler(IReadDbContext db, ChatAccess access, ICurrentUser currentUser)
    : IQueryHandler<GetMyConversationsQuery, IReadOnlyList<ConversationResponse>>
{
    public async Task<Result<IReadOnlyList<ConversationResponse>>> Handle(
        GetMyConversationsQuery query,
        CancellationToken cancellationToken)
    {
        var conversations = await access.VisibleTo(currentUser.UserId)
            .Where(c => c.Kind != ConversationKind.Board || db.Boards.Any(b => b.Id == c.BoardId && !b.IsArchived))
            .ToResponsesAsync(db, currentUser.UserId, cancellationToken);

        return conversations;
    }
}
