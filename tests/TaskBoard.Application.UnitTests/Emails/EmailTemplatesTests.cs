using System.Text.RegularExpressions;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.UnitTests.Emails;

public class EmailTemplatesTests
{
    private const string To = "ana@example.com";
    private const string Name = "Ana López";
    private const string App = "https://taskboard.example.com";

    private static readonly Guid BoardId = Guid.Parse("0199a000-0000-7000-8000-000000000001");
    private static readonly Guid CardId = Guid.Parse("0199a000-0000-7000-8000-000000000002");
    private static readonly Guid TeamId = Guid.Parse("0199a000-0000-7000-8000-000000000003");

    private readonly NotificationEmails _notificationEmails;
    private readonly AccountEmails _accountEmails;

    public EmailTemplatesTests()
    {
        var links = Substitute.For<IWebAppLinks>();
        links.To(Arg.Any<string>()).Returns(call => App + call.Arg<string>());

        _notificationEmails = new NotificationEmails(links);
        _accountEmails = new AccountEmails(links);
    }

    private static NotificationEmailFacts FactsFor(NotificationType type) => type switch
    {
        NotificationType.AddedToBoard => NotificationEmailFacts.Board(BoardId, "Lanzamiento"),
        NotificationType.AddedToTeam => NotificationEmailFacts.Team(TeamId, "Producto"),
        NotificationType.CardAssigned => NotificationEmailFacts.Card(BoardId, CardId, "Preparar la demo"),
        NotificationType.CardDueSoon => NotificationEmailFacts.Card(BoardId, CardId, "Preparar la demo"),
        NotificationType.RemovedFromBoard => NotificationEmailFacts.RemovedFromBoard("Lanzamiento", "Laura Díaz"),
        NotificationType.RemovedFromTeam => NotificationEmailFacts.RemovedFromTeam("Producto", "Laura Díaz"),
        NotificationType.TeamRoleChanged => NotificationEmailFacts.TeamRole(TeamId, "Producto", "administrador", "Laura Díaz"),
        _ => NotificationEmailFacts.Card(BoardId, CardId, "Preparar la demo", "Laura Díaz")
    };

    private Dictionary<string, EmailMessage> EveryEmail()
    {
        var emails = new Dictionary<string, EmailMessage>
        {
            ["reset-password"] = _accountEmails.ResetPassword(To, Name, "token", TimeSpan.FromHours(1)),
            ["password-changed"] = _accountEmails.PasswordChanged(To, Name),
            ["welcome"] = _accountEmails.Welcome(To, Name, "token", TimeSpan.FromDays(7)),
            ["verify-email"] = _accountEmails.VerifyEmail(To, Name, "token", TimeSpan.FromDays(7))
        };

        foreach (var type in Enum.GetValues<NotificationType>())
        {
            emails[type.ToString()] = _notificationEmails.Create(type, To, Name, FactsFor(type));
        }

        return emails;
    }

    [Fact]
    public void EveryEmail_HasBothFormsWithNoValueLeftUnfilled()
    {
        foreach (var (reason, email) in EveryEmail())
        {
            email.To.ShouldBe(To, reason);
            email.Subject.ShouldNotBeNullOrWhiteSpace(reason);
            email.Subject.ShouldNotContain("{", customMessage: reason);

            email.TextBody.ShouldStartWith($"Hola {Name},", customMessage: reason);
            email.TextBody.ShouldNotContain("{", customMessage: reason);
            email.TextBody.ShouldNotContain("null", customMessage: reason);

            email.HtmlBody.ShouldContain("<html lang=\"es\">", customMessage: reason);
            email.HtmlBody.ShouldContain("TaskBoard", customMessage: reason);
            email.HtmlBody.ShouldContain(Name, customMessage: reason);
            email.HtmlBody.ShouldNotContain("null", customMessage: reason);

            // A brace followed by a letter would be a placeholder that was never filled; the CSS always has a space.
            Regex.IsMatch(email.HtmlBody, @"\{[A-Za-z]").ShouldBeFalse(reason);
        }
    }

    [Fact]
    public void EveryEmail_IsBuiltTheWayMailClientsSupport()
    {
        foreach (var (reason, email) in EveryEmail())
        {
            email.HtmlBody.ShouldContain("<table role=\"presentation\"", customMessage: reason);
            email.HtmlBody.ShouldNotContain("<script", customMessage: reason);
            email.HtmlBody.ShouldNotContain("<svg", customMessage: reason);
            email.HtmlBody.ShouldNotContain("oklch", customMessage: reason);
            email.HtmlBody.ShouldNotContain("class=\"bg-", customMessage: reason);
        }
    }

    [Theory]
    [InlineData(NotificationType.CardAssigned, "Te asignaron la tarjeta «Preparar la demo»", "/boards/0199a000-0000-7000-8000-000000000001/cards/0199a000-0000-7000-8000-000000000002")]
    [InlineData(NotificationType.Mentioned, "Laura Díaz te mencionó en «Preparar la demo»", "/boards/0199a000-0000-7000-8000-000000000001/cards/0199a000-0000-7000-8000-000000000002")]
    [InlineData(NotificationType.CommentAdded, "Laura Díaz comentó en «Preparar la demo»", "/boards/0199a000-0000-7000-8000-000000000001/cards/0199a000-0000-7000-8000-000000000002")]
    [InlineData(NotificationType.AddedToBoard, "Te agregaron al tablero «Lanzamiento»", "/boards/0199a000-0000-7000-8000-000000000001")]
    [InlineData(NotificationType.AddedToTeam, "Te agregaron al equipo «Producto»", "/teams/0199a000-0000-7000-8000-000000000003")]
    [InlineData(NotificationType.RemovedFromBoard, "Ya no estás en el tablero «Lanzamiento»", "/")]
    [InlineData(NotificationType.RemovedFromTeam, "Ya no estás en el equipo «Producto»", "/teams")]
    [InlineData(NotificationType.TeamRoleChanged, "Ahora eres administrador del equipo «Producto»", "/teams/0199a000-0000-7000-8000-000000000003")]
    [InlineData(NotificationType.CardDueSoon, "La tarjeta «Preparar la demo» vence pronto", "/boards/0199a000-0000-7000-8000-000000000001/cards/0199a000-0000-7000-8000-000000000002")]
    public void NotificationEmail_SaysWhatHappenedAndLinksToIt(NotificationType type, string subject, string path)
    {
        var email = _notificationEmails.Create(type, To, Name, FactsFor(type));

        email.Subject.ShouldBe(subject);
        email.TextBody.ShouldContain(App + path);
        email.HtmlBody.ShouldContain($"href=\"{App}{path}\"");
    }

    [Fact]
    public void WhatUsersWrote_CannotAddMarkupToTheEmail()
    {
        var facts = NotificationEmailFacts.Card(BoardId, CardId, "<a href=\"https://evil.example\">Soporte</a>", "<b>Eve</b>");

        var email = _notificationEmails.Create(
            NotificationType.Mentioned, To, "<img src=x onerror=alert(1)>", facts);

        email.HtmlBody.ShouldNotContain("<a href=\"https://evil.example\"");
        email.HtmlBody.ShouldNotContain("<img src=x");
        email.HtmlBody.ShouldNotContain("<b>Eve");
        email.HtmlBody.ShouldContain("&lt;a href=&quot;https://evil.example&quot;&gt;");
        email.HtmlBody.ShouldContain("&lt;img src=x");

        // The text form is plain text: it shows what was written, as written.
        email.TextBody.ShouldContain("<b>Eve</b>");
    }

    [Fact]
    public void Subject_StaysOnOneLine()
    {
        var facts = NotificationEmailFacts.Card(BoardId, CardId, "Primera\r\nBcc: eve@example.com");

        var email = _notificationEmails.Create(NotificationType.CardAssigned, To, Name, facts);

        email.Subject.ShouldBe("Te asignaron la tarjeta «Primera Bcc: eve@example.com»");
    }

    [Fact]
    public void ResetLink_ReachesBothFormsAndKeepsTheTokenInTheFragment()
    {
        var email = _accountEmails.ResetPassword(To, Name, "abc_-123", TimeSpan.FromMinutes(30));

        const string link = App + "/reset-password#token=abc_-123";
        email.TextBody.ShouldContain(link);
        email.HtmlBody.ShouldContain($"href=\"{link}\"");
        email.TextBody.ShouldContain("30 minutos");
    }

    [Theory]
    [InlineData(1, "1 minuto")]
    [InlineData(45, "45 minutos")]
    [InlineData(60, "1 hora")]
    [InlineData(90, "90 minutos")]
    [InlineData(120, "2 horas")]
    public void ResetEmail_WritesHowLongTheLinkLasts(int minutes, string expected)
    {
        var email = _accountEmails.ResetPassword(To, Name, "token", TimeSpan.FromMinutes(minutes));

        email.TextBody.ShouldContain($"vence en {expected} ");
    }

    [Fact]
    public void OnlyOptionalNotices_LinkToTheirSettings()
    {
        var settings = App + "/account";

        _notificationEmails.Create(NotificationType.CardAssigned, To, Name, FactsFor(NotificationType.CardAssigned))
            .TextBody.ShouldContain(settings);
        _accountEmails.PasswordChanged(To, Name).TextBody.ShouldNotContain(settings);
        _accountEmails.ResetPassword(To, Name, "token", TimeSpan.FromHours(1)).TextBody.ShouldNotContain(settings);
    }

    // Emoji look different in every client and some show them as empty boxes, so icons are typographic marks.
    [Fact]
    public void EveryEmail_IsSignedLabeledAndFreeOfEmoji()
    {
        foreach (var (reason, email) in EveryEmail())
        {
            email.HtmlBody.ShouldContain("El equipo de TaskBoard", customMessage: reason);
            email.TextBody.ShouldContain("El equipo de TaskBoard", customMessage: reason);
            email.HtmlBody.ShouldContain("text-transform:uppercase", customMessage: reason);

            var emoji = email.HtmlBody.EnumerateRunes().Where(IsEmoji).Select(rune => rune.ToString());
            emoji.ShouldBeEmpty(reason);
        }
    }

    private static bool IsEmoji(System.Text.Rune rune) =>
        rune.Value >= 0x1F000 || rune.Value == 0xFE0F || rune.Value is >= 0x2600 and <= 0x26FF
        || rune.Value is 0x2709 or 0x2714;

    [Fact]
    public void NotificationEmail_DrawsWhatItIsAboutAsACardWithWhoActed()
    {
        var email = _notificationEmails.Create(
            NotificationType.Mentioned, To, Name, FactsFor(NotificationType.Mentioned));

        email.HtmlBody.ShouldContain(">Tarjeta<");
        email.HtmlBody.ShouldContain(">Preparar la demo<");
        email.HtmlBody.ShouldContain(">LD<");
        email.TextBody.ShouldContain("De: Laura Díaz\nTarjeta: Preparar la demo");
    }

    [Fact]
    public void VerificationLinks_KeepTheTokenInTheFragmentAndSayWhenTheyExpire()
    {
        const string link = App + "/verify-email#token=abc_-123";

        foreach (var email in new[]
        {
            _accountEmails.Welcome(To, Name, "abc_-123", TimeSpan.FromDays(7)),
            _accountEmails.VerifyEmail(To, Name, "abc_-123", TimeSpan.FromDays(7))
        })
        {
            email.TextBody.ShouldContain(link);
            email.HtmlBody.ShouldContain($"href=\"{link}\"");
            email.TextBody.ShouldContain("vence en 7 días");
            email.TextBody.ShouldNotContain(App + "/account");
        }
    }

    [Fact]
    public void RemovalEmail_NamesWhoRemovedTheUser()
    {
        var email = _notificationEmails.Create(
            NotificationType.RemovedFromBoard, To, Name, NotificationEmailFacts.RemovedFromBoard("Lanzamiento", "Laura Díaz"));

        email.TextBody.ShouldContain("Laura Díaz te quitó del tablero «Lanzamiento».");
    }

    [Fact]
    public void PasswordChanged_TellsHowToRecoverTheAccount()
    {
        var email = _accountEmails.PasswordChanged(To, Name);

        email.HtmlBody.ShouldContain($"href=\"{App}/forgot-password\"");
        email.TextBody.ShouldContain("¿No fuiste tú?");
    }

    // Not an assertion: leaves every design next to the test binaries, with index.html showing them all at
    // desktop and phone width, to open in a browser while working on them.
    [Fact]
    public void WritesPreviews()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "email-previews");
        Directory.CreateDirectory(folder);

        var gallery = new System.Text.StringBuilder(
            """
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><title>TaskBoard: correos</title>
            <style>
            body{margin:0;padding:32px;background:#e5e7eb;font:14px -apple-system,'Segoe UI',Roboto,sans-serif;color:#141822}
            h2{margin:40px 0 12px;font-size:16px}
            .row{display:flex;gap:24px;align-items:flex-start;flex-wrap:wrap}
            iframe{border:1px solid #cbd5e1;background:#fff;border-radius:8px}
            </style></head><body><h1>TaskBoard: correos</h1>

            """);

        foreach (var (reason, email) in EveryEmail())
        {
            File.WriteAllText(Path.Combine(folder, $"{reason}.html"), email.HtmlBody);
            File.WriteAllText(Path.Combine(folder, $"{reason}.txt"), $"Asunto: {email.Subject}\n\n{email.TextBody}");
            gallery.Append(
                $"""
                <h2>{System.Net.WebUtility.HtmlEncode(email.Subject)}</h2><div class="row">
                <iframe src="{reason}.html" width="680" height="1000" title="{reason}"></iframe>
                <iframe src="{reason}.html" width="375" height="1000" title="{reason}"></iframe></div>

                """);
        }

        File.WriteAllText(Path.Combine(folder, "index.html"), gallery.Append("</body></html>\n").ToString());

        Directory.GetFiles(folder, "*.html").Length.ShouldBeGreaterThanOrEqualTo(EveryEmail().Count);
    }
}
