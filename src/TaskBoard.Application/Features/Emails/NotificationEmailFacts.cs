namespace TaskBoard.Application.Features.Emails;

/// <summary>
/// What a notification's email needs to know. The in-app message is a finished English sentence,
/// so the email is written from these facts instead.
/// </summary>
/// <param name="ResourceName">The title of the card, or the name of the board or team.</param>
/// <param name="Path">Where the email's button leads in the web app.</param>
/// <param name="ActorName">Who did it, when the email names them.</param>
/// <param name="Detail">What else the email says, such as the new role.</param>
internal sealed record NotificationEmailFacts(string ResourceName, string Path, string? ActorName = null, string? Detail = null)
{
    public static NotificationEmailFacts Card(Guid boardId, Guid cardId, string title, string? actorName = null) =>
        new(title, $"/boards/{boardId}/cards/{cardId}", actorName);

    public static NotificationEmailFacts Board(Guid boardId, string name) => new(name, $"/boards/{boardId}");

    public static NotificationEmailFacts Team(Guid teamId, string name) => new(name, $"/teams/{teamId}");

    // The user can no longer open it, so the button leads to their own list.
    public static NotificationEmailFacts RemovedFromBoard(string name, string? actorName) => new(name, "/", actorName);

    public static NotificationEmailFacts RemovedFromTeam(string name, string? actorName) => new(name, "/teams", actorName);

    public static NotificationEmailFacts TeamRole(Guid teamId, string name, string roleName, string? actorName) =>
        new(name, $"/teams/{teamId}", actorName, roleName);
}
