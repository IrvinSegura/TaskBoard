using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Activities;

/// <summary>
/// One line of a board's history. Names are copied when it is written, so the line still reads well
/// after the card, column or person it mentions is renamed or deleted.
/// </summary>
public sealed class ActivityEntry : Entity
{
    public const int SubjectMaxLength = 200;
    public const int DetailMaxLength = 300;

    private ActivityEntry()
    {
    }

    public Guid BoardId { get; private set; }

    public Guid? CardId { get; private set; }

    // Null when the change did not come from a signed-in request.
    public Guid? ActorId { get; private set; }

    public ActivityType Type { get; private set; }

    // What changed: the card title, the column or board name.
    public string? Subject { get; private set; }

    // The rest of the sentence: target column, person, file name or a comment excerpt.
    public string? Detail { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static ActivityEntry Create(
        Guid boardId,
        Guid? cardId,
        Guid? actorId,
        ActivityType type,
        string? subject,
        string? detail,
        DateTimeOffset occurredAt) => new()
    {
        BoardId = boardId,
        CardId = cardId,
        ActorId = actorId,
        Type = type,
        Subject = Truncate(subject, SubjectMaxLength),
        Detail = Truncate(detail, DetailMaxLength),
        OccurredAt = occurredAt
    };

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 1), "…");
}
