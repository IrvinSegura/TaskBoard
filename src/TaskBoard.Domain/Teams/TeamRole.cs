namespace TaskBoard.Domain.Teams;

// Ordered by privilege so roles can be compared with >=.
public enum TeamRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}
