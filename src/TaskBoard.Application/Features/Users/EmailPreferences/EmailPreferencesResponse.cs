using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Users.EmailPreferences;

// The notifications the user does not want by email. Anything not listed is sent.
public sealed record EmailPreferencesResponse(IReadOnlyList<NotificationType> MutedTypes);
