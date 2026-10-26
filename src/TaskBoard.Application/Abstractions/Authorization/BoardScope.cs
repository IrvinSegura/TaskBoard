namespace TaskBoard.Application.Abstractions.Authorization;

// Every resource that lives inside a board; each one resolves to its board id.
public enum BoardScope
{
    Board,
    Column,
    Card,
    Comment,
    Attachment
}
