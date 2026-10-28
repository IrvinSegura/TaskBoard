namespace TaskBoard.Domain.Chat;

public enum ConversationKind
{
    // Two people. Their participants never change.
    Direct = 0,

    // A named conversation between the people someone picked.
    Group = 1,

    // Everyone who can open the board; nobody is stored as a participant.
    Board = 2,

    // Every member of the team; nobody is stored as a participant.
    Team = 3
}
