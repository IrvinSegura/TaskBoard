using TaskBoard.Domain.Checklists.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Checklists;

// Own aggregate, like comments: checking an item off never loads the card or the rest of the list.
// The caller checks that the card is not archived.
public sealed class ChecklistItem : AggregateRoot
{
    private ChecklistItem()
    {
    }

    public Guid CardId { get; private set; }

    // Denormalized so the board view can count progress per card without joining through cards.
    public Guid BoardId { get; private set; }

    public string Text { get; private set; } = null!;

    public bool IsDone { get; private set; }

    public double Position { get; private set; }

    public static ChecklistItem Create(Guid cardId, Guid boardId, string text, double position)
    {
        var item = new ChecklistItem { CardId = cardId, BoardId = boardId, Text = text.Trim(), Position = position };
        item.Raise(new ChecklistItemAddedEvent(item.Id, cardId, boardId));
        return item;
    }

    // Null leaves that part as it is, so checking an item off does not resend its text.
    public void Update(string? text, bool? isDone)
    {
        Text = text?.Trim() ?? Text;
        IsDone = isDone ?? IsDone;
        Raise(new ChecklistItemUpdatedEvent(Id, CardId, BoardId));
    }

    public void Delete() => Raise(new ChecklistItemDeletedEvent(Id, CardId, BoardId));
}
