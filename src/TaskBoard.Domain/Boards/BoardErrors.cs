using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards;

public static class BoardErrors
{
    public static readonly Error Archived =
        Error.Failure("Boards.Archived", "The board is archived and cannot be modified.");

    public static readonly Error AlreadyMember =
        Error.Conflict("Boards.AlreadyMember", "The user is already a member of the board.");

    public static readonly Error MemberNotFound =
        Error.NotFound("Boards.MemberNotFound", "The user is not a member of the board.");

    public static readonly Error LastAdmin =
        Error.Failure("Boards.LastAdmin", "A board must keep at least one admin.");

    public static readonly Error UserNotInTeam =
        Error.Validation("Boards.UserNotInTeam", "Only members of the board team can be added.");

    public static Error NotFound(Guid boardId) =>
        Error.NotFound("Boards.NotFound", $"Board '{boardId}' was not found.");

    public static Error ColumnNotFound(Guid columnId) =>
        Error.NotFound("Boards.ColumnNotFound", $"Column '{columnId}' was not found.");
}
