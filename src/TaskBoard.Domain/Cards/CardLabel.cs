using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards;

public sealed class CardLabel : Entity
{
    private CardLabel()
    {
    }

    internal CardLabel(Guid cardId, Guid labelId)
    {
        CardId = cardId;
        LabelId = labelId;
    }

    public Guid CardId { get; private set; }

    public Guid LabelId { get; private set; }
}
