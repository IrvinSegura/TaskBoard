using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards;

public sealed class BoardMember : Entity
{
    private BoardMember()
    {
    }

    internal BoardMember(Guid boardId, Guid userId, BoardRole role)
    {
        BoardId = boardId;
        UserId = userId;
        Role = role;
    }

    public Guid BoardId { get; private set; }

    public Guid UserId { get; private set; }

    public BoardRole Role { get; private set; }
}
