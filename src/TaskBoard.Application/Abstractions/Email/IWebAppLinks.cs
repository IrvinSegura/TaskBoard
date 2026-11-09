namespace TaskBoard.Application.Abstractions.Email;

/// <summary>
/// Absolute links to the web app. They come from configuration, never from the request's Host header,
/// so a forged header cannot turn an email's link into someone else's site.
/// </summary>
public interface IWebAppLinks
{
    string To(string path);
}
