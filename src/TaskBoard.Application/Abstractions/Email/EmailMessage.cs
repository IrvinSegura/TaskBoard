namespace TaskBoard.Application.Abstractions.Email;

// Both forms of the body travel together: clients show the HTML one, or the text one if they do not render HTML.
public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);
