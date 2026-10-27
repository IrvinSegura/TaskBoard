using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Checklists;

public static class ChecklistErrors
{
    public static Error ItemNotFound(Guid itemId) =>
        Error.NotFound("Checklists.ItemNotFound", $"Checklist item '{itemId}' was not found.");
}
