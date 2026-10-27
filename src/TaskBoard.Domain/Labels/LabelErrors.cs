using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Labels;

public static class LabelErrors
{
    public static Error NotFound(Guid labelId) =>
        Error.NotFound("Labels.NotFound", $"Label '{labelId}' was not found.");
}
