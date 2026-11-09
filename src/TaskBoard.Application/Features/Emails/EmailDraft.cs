namespace TaskBoard.Application.Features.Emails;

// The mood of an email, which sets its colors.
internal enum EmailTone
{
    Info,
    Warning
}

internal sealed record EmailDetail(string Label, string Value);

internal sealed record EmailAction(string Label, string Url);

/// <summary>
/// The card, board or team an email is about, drawn the way the board shows it.
/// </summary>
/// <param name="Kind">What it is, such as "Tarjeta".</param>
/// <param name="Name">Its title or name, as the user wrote it.</param>
/// <param name="ActorName">Who acted on it, when the email names them.</param>
internal sealed record EmailCardPreview(string Kind, string Name, string? ActorName = null);

/// <summary>
/// What one email says, before <see cref="EmailLayout"/> gives it its HTML and text forms. Every value is
/// plain text, including what users wrote such as names and card titles: the layout escapes it.
/// </summary>
internal sealed class EmailDraft
{
    public required EmailTone Tone { get; init; }

    // A typographic mark, never an emoji: emoji look different in every client and some show them as empty boxes.
    public required string Icon { get; init; }

    // The short label above the title that says what kind of email it is.
    public required string Kicker { get; init; }

    public required string Subject { get; init; }

    // The line clients show next to the subject in the inbox.
    public required string Preheader { get; init; }

    public required string Title { get; init; }

    public required string Greeting { get; init; }

    public IReadOnlyList<string> Paragraphs { get; init; } = [];

    public EmailCardPreview? Card { get; init; }

    public IReadOnlyList<EmailDetail> Details { get; init; } = [];

    public EmailAction? Action { get; init; }

    // Shown with the action's address written out, for clients that break the button. Only for important links.
    public string? LinkHint { get; init; }

    public string? CalloutTitle { get; init; }

    public string? Callout { get; init; }

    public IReadOnlyList<string> FooterLines { get; init; } = [];

    public EmailAction? FooterLink { get; init; }
}
