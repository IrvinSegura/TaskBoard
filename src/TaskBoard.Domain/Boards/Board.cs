using TaskBoard.Domain.Boards.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards;

public sealed class Board : AggregateRoot
{
    private readonly List<BoardMember> _members = [];
    private readonly List<Column> _columns = [];

    private Board()
    {
    }

    public Guid TeamId { get; private set; }

    public string Name { get; private set; } = null!;

    public BoardVisibility Visibility { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<BoardMember> Members => _members.AsReadOnly();

    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    public static Board Create(Guid teamId, string name, Guid creatorId, BoardVisibility visibility)
    {
        var board = new Board { TeamId = teamId, Name = name.Trim(), Visibility = visibility };
        board._members.Add(new BoardMember(board.Id, creatorId, BoardRole.Admin));
        return board;
    }

    public Result Rename(string name)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        Name = name.Trim();
        Raise(new BoardRenamedEvent(Id, Name));
        return Result.Success();
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        Raise(new BoardArchivedEvent(Id));
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        Raise(new BoardRestoredEvent(Id));
    }

    // Archiving first is the safety net: a board in use cannot be deleted by mistake.
    public Result Delete()
    {
        if (!IsArchived)
        {
            return BoardErrors.NotArchived;
        }

        Raise(new BoardDeletedEvent(Id));
        return Result.Success();
    }

    public Result AddMember(Guid userId, BoardRole role)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        if (_members.Any(m => m.UserId == userId))
        {
            return BoardErrors.AlreadyMember;
        }

        _members.Add(new BoardMember(Id, userId, role));
        Raise(new BoardMemberAddedEvent(Id, userId, role));
        return Result.Success();
    }

    public Result RemoveMember(Guid userId)
    {
        var member = _members.SingleOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return BoardErrors.MemberNotFound;
        }

        if (member.Role == BoardRole.Admin && _members.Count(m => m.Role == BoardRole.Admin) == 1)
        {
            return BoardErrors.LastAdmin;
        }

        _members.Remove(member);
        Raise(new BoardMemberRemovedEvent(Id, userId));
        return Result.Success();
    }

    public Result<Column> AddColumn(string name)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        double? lastPosition = _columns.Count == 0 ? null : _columns.Max(c => c.Position);
        var column = new Column(Id, name.Trim(), PositionCalculator.After(lastPosition));
        _columns.Add(column);

        Raise(new ColumnCreatedEvent(Id, column.Id, column.Name, column.Position));
        return column;
    }

    public Result RenameColumn(Guid columnId, string name)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        var column = _columns.SingleOrDefault(c => c.Id == columnId);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(columnId);
        }

        column.Rename(name.Trim());
        Raise(new ColumnRenamedEvent(Id, column.Id, column.Name));
        return Result.Success();
    }

    /// <summary>
    /// Places a column between two neighbours of this board (both null when it is the only column).
    /// </summary>
    public Result MoveColumn(Guid columnId, Guid? previousColumnId, Guid? nextColumnId)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        var column = _columns.SingleOrDefault(c => c.Id == columnId);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(columnId);
        }

        var previous = FindNeighbour(previousColumnId, columnId);
        var next = FindNeighbour(nextColumnId, columnId);
        if (previous.IsMissing || next.IsMissing)
        {
            return BoardErrors.InvalidColumnNeighbour;
        }

        if (PositionCalculator.NeedsRebalance(previous.Column?.Position, next.Column?.Position))
        {
            RenumberColumns(except: column);
        }

        column.MoveTo(PositionCalculator.Between(previous.Column?.Position, next.Column?.Position));
        Raise(new ColumnMovedEvent(Id, column.Id, column.Position));
        return Result.Success();
    }

    /// <summary>
    /// Removes the column; the database deletes its cards, archived ones included, with it.
    /// </summary>
    public Result DeleteColumn(Guid columnId)
    {
        if (IsArchived)
        {
            return BoardErrors.Archived;
        }

        var column = _columns.SingleOrDefault(c => c.Id == columnId);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(columnId);
        }

        _columns.Remove(column);
        Raise(new ColumnDeletedEvent(Id, column.Id));
        return Result.Success();
    }

    // Spreads the other columns evenly again, keeping their order, once positions ran out of precision.
    // Each one raises ColumnMoved so clients pick up the new positions.
    private void RenumberColumns(Column except)
    {
        var ordered = _columns.Where(c => c != except).OrderBy(c => c.Position).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].MoveTo(PositionCalculator.Sequential(i));
            Raise(new ColumnMovedEvent(Id, ordered[i].Id, ordered[i].Position));
        }
    }

    // A neighbour id that is not another column of this board is missing, not ignored.
    private (Column? Column, bool IsMissing) FindNeighbour(Guid? neighbourId, Guid movingColumnId)
    {
        if (neighbourId is null)
        {
            return (null, false);
        }

        var neighbour = _columns.SingleOrDefault(c => c.Id == neighbourId && c.Id != movingColumnId);
        return (neighbour, neighbour is null);
    }
}
