namespace TaskBoard.Domain.Activities;

public enum ActivityType
{
    CardCreated = 0,
    CardUpdated = 1,
    CardMoved = 2,
    CardArchived = 3,
    CardRestored = 4,
    CardDeleted = 5,
    CardAssigned = 6,
    CardUnassigned = 7,
    CommentAdded = 8,
    AttachmentAdded = 9,
    ColumnCreated = 10,
    ColumnRenamed = 11,
    ColumnDeleted = 12,
    BoardRenamed = 13,
    BoardArchived = 14,
    BoardRestored = 15,
    MemberAdded = 16,
    MemberRemoved = 17
}
