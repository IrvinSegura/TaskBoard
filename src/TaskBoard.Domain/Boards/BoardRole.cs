namespace TaskBoard.Domain.Boards;

// Ordered by privilege so roles can be compared with >=.
public enum BoardRole
{
    Viewer = 0,
    Editor = 1,
    Admin = 2
}
