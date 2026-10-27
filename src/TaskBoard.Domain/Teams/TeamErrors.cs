using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams;

public static class TeamErrors
{
    public static readonly Error NotAllowed =
        Error.Forbidden("Teams.NotAllowed", "Only team owners and admins can manage the team.");

    public static readonly Error OnlyOwnerCanDelete =
        Error.Forbidden("Teams.OnlyOwnerCanDelete", "Only the team owner can delete the team.");

    public static readonly Error AlreadyMember =
        Error.Conflict("Teams.AlreadyMember", "The user is already a member of the team.");

    public static readonly Error MemberNotFound =
        Error.NotFound("Teams.MemberNotFound", "The user is not a member of the team.");

    public static readonly Error CannotAssignOwner =
        Error.Validation("Teams.CannotAssignOwner", "The owner role cannot be assigned or changed.");

    public static readonly Error CannotRemoveOwner =
        Error.Failure("Teams.CannotRemoveOwner", "The team owner cannot be removed.");

    public static Error NotFound(Guid teamId) =>
        Error.NotFound("Teams.NotFound", $"Team '{teamId}' was not found.");
}
