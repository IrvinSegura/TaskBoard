using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards;

public sealed class Column : Entity
{
    private Column()
    {
    }

    internal Column(Guid boardId, string name, double position)
    {
        BoardId = boardId;
        Name = name;
        Position = position;
    }

    public Guid BoardId { get; private set; }

    public string Name { get; private set; } = null!;

    public double Position { get; private set; }

    internal void Rename(string name) => Name = name;

    internal void MoveTo(double position) => Position = position;
}
