namespace TaskBoard.Api.Realtime;

internal static class HubGroups
{
    public static string Board(Guid boardId) => $"board:{boardId}";
}
