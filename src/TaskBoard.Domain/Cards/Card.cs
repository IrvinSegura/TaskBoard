using TaskBoard.Domain.Cards.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards;

public sealed class Card : AggregateRoot
{
    private readonly List<CardAssignment> _assignments = [];

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

        Raise(new CardUpdatedEvent(Id, BoardId));
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
        var assignment = _assignments.SingleOrDefault(a => a.UserId == userId);
        if (assignment is null)
        {
            return CardErrors.NotAssigned;
        }

        _assignments.Remove(assignment);
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
}
