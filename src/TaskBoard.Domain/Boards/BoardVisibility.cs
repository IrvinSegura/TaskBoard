namespace TaskBoard.Domain.Boards;

public enum BoardVisibility
{
    // Only explicit board members can see it.
    Private = 0,

    // Every team member can see it as a viewer.
    Team = 1
}
