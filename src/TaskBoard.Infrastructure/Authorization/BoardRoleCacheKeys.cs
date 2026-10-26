namespace TaskBoard.Infrastructure.Authorization;

internal static class BoardRoleCacheKeys
{
    public static string Role(Guid boardId, Guid userId) => $"board-role:{boardId}:{userId}";
}
