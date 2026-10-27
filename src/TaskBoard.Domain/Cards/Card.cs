using TaskBoard.Domain.Cards.Events;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Domain.Cards;

public sealed class Card : AggregateRoot
{
    private readonly List<CardAssignment> _assignments = [];
    private readonly List<CardLabel> _labels = [];

    private Card()
    {
    }

    public Guid BoardId { get; private set; }

    public Guid ColumnId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public CardPriority Priority { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public double Position { get; private set; }

    public bool IsArchived { get; private set; }

    public Guid CreatedById { get; private set; }

    public IReadOnlyCollection<CardAssignment> Assignments => _assignments.AsReadOnly();

    public IReadOnlyCollection<CardLabel> Labels => _labels.AsReadOnly();

    public static Card Create(
        Guid boardId,
        Guid columnId,
        string title,
        string? description,
        Guid createdById,
        double position)
    {
        var card = new Card
        {
            BoardId = boardId,
            ColumnId = columnId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Priority = CardPriority.None,
            Position = position,
            CreatedById = createdById
        };

        card.Raise(new CardCreatedEvent(card.Id, boardId, columnId, card.Title, position));
        return card;
    }

    public Result Update(string title, string? description, CardPriority priority, DateTimeOffset? dueDate)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        DueDate = dueDate;

        Raise(new CardUpdatedEvent(Id, BoardId, Title, Description, Priority, DueDate));
        return Result.Success();
    }

    public Result MoveTo(Guid columnId, double position)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        var fromColumnId = ColumnId;
        ColumnId = columnId;
        Position = position;

        Raise(new CardMovedEvent(Id, BoardId, fromColumnId, columnId, position));
        return Result.Success();
    }

    /// <summary>
    /// Renumbering after positions in the column ran out of precision: same column, same relative order.
    /// Archived cards are renumbered too, so they come back in the right place when restored.
    /// </summary>
    public void Reposition(double position)
    {
        Position = position;

        if (!IsArchived)
        {
            Raise(new CardMovedEvent(Id, BoardId, ColumnId, ColumnId, position));
        }
    }

    public Result Assign(Guid userId, Guid assignedById)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        if (_assignments.Any(a => a.UserId == userId))
        {
            return CardErrors.AlreadyAssigned;
        }

        _assignments.Add(new CardAssignment(Id, userId));
        Raise(new CardAssignedEvent(Id, BoardId, Title, userId, assignedById));
        return Result.Success();
    }

    public Result Unassign(Guid userId)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        var assignment = _assignments.SingleOrDefault(a => a.UserId == userId);
        if (assignment is null)
        {
            return CardErrors.NotAssigned;
        }

        _assignments.Remove(assignment);
        Raise(new CardUnassignedEvent(Id, BoardId, userId));
        return Result.Success();
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        Raise(new CardArchivedEvent(Id, BoardId));
    }

    public Result AddLabel(Label label)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        if (label.BoardId != BoardId)
        {
            return CardErrors.LabelNotOnBoard;
        }

        if (_labels.Any(l => l.LabelId == label.Id))
        {
            return CardErrors.LabelAlreadyAdded;
        }

        _labels.Add(new CardLabel(Id, label.Id));
        Raise(new CardLabelsChangedEvent(Id, BoardId));
        return Result.Success();
    }

    public Result RemoveLabel(Guid labelId)
    {
        if (IsArchived)
        {
            return CardErrors.Archived;
        }

        var cardLabel = _labels.SingleOrDefault(l => l.LabelId == labelId);
        if (cardLabel is null)
        {
            return CardErrors.LabelNotAdded;
        }

        _labels.Remove(cardLabel);
        Raise(new CardLabelsChangedEvent(Id, BoardId));
        return Result.Success();
    }

    // Only archived cards can be deleted, so a card in use is never lost by mistake.
    public Result Delete()
    {
        if (!IsArchived)
        {
            return CardErrors.NotArchived;
        }

        Raise(new CardDeletedEvent(Id, BoardId));
        return Result.Success();
    }

    // Returns to the column and position it had when archived.
    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        Raise(new CardRestoredEvent(Id, BoardId, ColumnId, Title, Position));
    }
}
