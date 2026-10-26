using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards;

public sealed class CardAssignment : Entity
{
    private CardAssignment()
    {
    }

    internal CardAssignment(Guid cardId, Guid userId)
    {
        CardId = cardId;
        UserId = userId;
    }

    public Guid CardId { get; private set; }

    public Guid UserId { get; private set; }
}
