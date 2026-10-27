using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels.Events;

namespace TaskBoard.Domain.Labels;

// Own aggregate (not inside Board): editing a label never loads the board with its members and columns.
public sealed class Label : AggregateRoot
{
    private Label()
    {
    }

    public Guid BoardId { get; private set; }

    public string Name { get; private set; } = null!;

    public LabelColor Color { get; private set; }

    public static Label Create(Guid boardId, string name, LabelColor color)
    {
        var label = new Label { BoardId = boardId, Name = name.Trim(), Color = color };
        label.Raise(new LabelCreatedEvent(label.Id, boardId));
        return label;
    }

    public void Update(string name, LabelColor color)
    {
        Name = name.Trim();
        Color = color;
        Raise(new LabelUpdatedEvent(Id, BoardId));
    }

    // The database removes the label from every card that had it.
    public void Delete() => Raise(new LabelDeletedEvent(Id, BoardId));
}
