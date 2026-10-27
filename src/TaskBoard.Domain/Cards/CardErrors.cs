using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards;

public static class CardErrors
{
    public static readonly Error Archived =
        Error.Failure("Cards.Archived", "The card is archived and cannot be modified.");

    public static readonly Error NotArchived =
        Error.Failure("Cards.NotArchived", "Archive the card before deleting it.");

    public static readonly Error AlreadyAssigned =
        Error.Conflict("Cards.AlreadyAssigned", "The user is already assigned to the card.");

    public static readonly Error NotAssigned =
        Error.NotFound("Cards.NotAssigned", "The user is not assigned to the card.");

    public static readonly Error InvalidTargetColumn =
        Error.Validation("Cards.InvalidTargetColumn", "The target column does not belong to the card board.");

    public static readonly Error AssigneeNotBoardMember =
        Error.Validation("Cards.AssigneeNotBoardMember", "Only board members can be assigned to a card.");

    public static Error NotFound(Guid cardId) =>
        Error.NotFound("Cards.NotFound", $"Card '{cardId}' was not found.");
}
