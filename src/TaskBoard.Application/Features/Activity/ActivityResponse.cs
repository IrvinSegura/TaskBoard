using TaskBoard.Domain.Activities;

namespace TaskBoard.Application.Features.Activity;

public sealed record ActivityResponse(
    Guid Id,
    ActivityType Type,
    Guid? ActorId,
    string? ActorName,
    Guid BoardId,
    string BoardName,
    Guid? CardId,
    string? Subject,
    string? Detail,
    DateTimeOffset OccurredAt);
