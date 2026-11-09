using System.Text;
using System.Text.RegularExpressions;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Application.Features.Emails;

/// <summary>
/// Turns an <see cref="EmailDraft"/> into the email's two forms. The HTML is designed as a piece of the board:
/// what the email is about (a card, a board or a team) is drawn as a card resting in a column, with its label,
/// its name and who acted on it, the way it looks in the app.
/// </summary>
/// <remarks>
/// It follows what mail clients actually support: nested tables, inline styles and system fonts. Nothing depends
/// on an image, because most clients block them by default; icons are typographic marks, which unlike emoji look
/// the same everywhere. Light only, because clients that invert colors on their own would undo a dark version
/// anyway. Every value is escaped on its way in, so a card called <c>&lt;b&gt;Ship&lt;/b&gt;</c> shows up as
/// written and cannot add markup or links to an email sent from our address.
/// </remarks>
internal static partial class EmailLayout
{
    private const string Brand = "TaskBoard";
    private const string SignOff = "El equipo de TaskBoard";
    private const string Font = "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    // The web app's palette (web/src/styles/index.css) in hex, since mail clients do not understand oklch.
    private const string Primary = "#4f46e5";
    private const string PrimarySoft = "#ecf1ff";
    private const string Text = "#141822";
    private const string Body = "#3f4550";
    private const string Muted = "#555b66";
    private const string Line = "#dfe1e5";
    private const string Page = "#f3f4f6";
    private const string Column = "#eef0f4";
    private const string Surface = "#ffffff";

    public static EmailMessage Message(string to, EmailDraft draft) =>
        new(to, OneLine(draft.Subject), TextForm(draft), HtmlForm(draft));

    // A title may hold a line break, and a subject must stay on the one header line it belongs to.
    private static string OneLine(string text) => WhitespaceRun().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    private static string TextForm(EmailDraft draft)
    {
        var blocks = new List<string> { draft.Greeting };
        blocks.AddRange(draft.Paragraphs);

        if (draft.Card is { } card)
        {
            var lines = new List<string>();
            if (card.ActorName is { } actor)
            {
                lines.Add($"De: {actor}");
            }

            lines.Add($"{card.Kind}: {card.Name}");
            blocks.Add(string.Join('\n', lines));
        }

        if (draft.Details.Count > 0)
        {
            blocks.Add(string.Join('\n', draft.Details.Select(d => $"{d.Label}: {d.Value}")));
        }

        if (draft.Action is { } action)
        {
            blocks.Add($"{action.Label}:\n{action.Url}");
        }

        if (draft.Callout is { } callout)
        {
            blocks.Add(draft.CalloutTitle is { } title ? $"{title} {callout}" : callout);
        }

        blocks.Add(SignOff);

        var footer = new List<string> { "--", Brand };
        footer.AddRange(draft.FooterLines);
        if (draft.FooterLink is { } link)
        {
            footer.Add($"{link.Label}: {link.Url}");
        }

        blocks.Add(string.Join('\n', footer));

        return string.Join("\n\n", blocks) + "\n";
    }

    private static string HtmlForm(EmailDraft draft)
    {
        var (accent, soft) = Colors(draft.Tone);

        var body = new StringBuilder();
        body.Append(KickerBlock(draft, accent, soft));
        body.Append(Block(
            $"""<h1 class="headline" style="margin:0;font-family:{Font};font-size:28px;line-height:35px;font-weight:700;letter-spacing:-0.4px;color:{Text};">{Escape(draft.Title)}</h1>""",
            16));
        body.Append(Paragraph(draft.Greeting, Text, 600, 22));

        foreach (var paragraph in draft.Paragraphs)
        {
            body.Append(Paragraph(paragraph, Body, 400, 10));
        }

        if (draft.Card is { } card)
        {
            body.Append(CardBlock(card));
        }

        if (draft.Details.Count > 0)
        {
            body.Append(DetailsBlock(draft.Details));
        }

        if (draft.Action is { } action)
        {
            body.Append(Button(action));

            if (draft.LinkHint is { } hint)
            {
                body.Append(LinkFallback(hint, action.Url));
            }
        }

        if (draft.Callout is { } callout)
        {
            body.Append(CalloutBlock(draft, callout, accent, soft));
        }

        body.Append(Block(
            $"""<p style="margin:0;font-family:{Font};font-size:15px;line-height:22px;color:{Body};">{SignOff}</p>""",
            30));

        return $$"""
            <!doctype html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="x-apple-disable-message-reformatting">
            <meta name="color-scheme" content="light">
            <meta name="supported-color-schemes" content="light">
            <title>{{Escape(OneLine(draft.Subject))}}</title>
            <style>
              @media only screen and (max-width: 600px) {
                .page { padding: 0 !important; }
                .card { border-radius: 0 !important; border-left: 0 !important; border-right: 0 !important; }
                .pad { padding-left: 24px !important; padding-right: 24px !important; }
                .headline { font-size: 24px !important; line-height: 31px !important; }
                .button { width: 100% !important; }
                .button a { display: block !important; text-align: center !important; }
              }
            </style>
            </head>
            <body style="margin:0;padding:0;background-color:{{Page}};-webkit-text-size-adjust:100%;">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:{{Page}};">{{Escape(draft.Preheader)}}</div>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="{{Page}}" style="background-color:{{Page}};">
            <tr><td align="center" class="page" style="padding:40px 16px;">
            <!--[if mso]><table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0"><tr><td><![endif]-->
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:600px;">
            <tr><td class="card" bgcolor="{{Surface}}" style="background-color:{{Surface}};border:1px solid {{Line}};border-top:4px solid {{Primary}};border-radius:12px;">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
            {{Header()}}{{body}}<tr><td style="height:36px;line-height:36px;font-size:0;">&nbsp;</td></tr>
            </table>
            </td></tr>
            {{Footer(draft)}}
            </table>
            <!--[if mso]></td></tr></table><![endif]-->
            </td></tr>
            </table>
            </body>
            </html>

            """;
    }

    private static (string Accent, string Soft) Colors(EmailTone tone) => tone switch
    {
        EmailTone.Warning => ("#b45309", "#fff2d0"),
        _ => (Primary, PrimarySoft)
    };

    // The app's logo drawn with table cells, three columns of a board: images are blocked by default in most
    // clients, and SVG in nearly all.
    private static string Header() =>
        $"""
        <tr><td class="pad" style="padding:24px 44px 0;">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"><tr><td style="padding-bottom:18px;border-bottom:1px solid {Line};">
        <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
        <td width="34" height="34" align="center" valign="middle" bgcolor="{Primary}" style="width:34px;height:34px;background-color:{Primary};border-radius:9px;">
        <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>{LogoBar(18, "#ffffff", 2)}{LogoBar(12, "#c7c4f7", 2)}{LogoBar(8, "#a6a2ef", 0)}</tr></table>
        </td>
        <td style="padding-left:12px;font-family:{Font};font-size:19px;line-height:24px;font-weight:700;letter-spacing:-0.3px;color:{Text};">{Brand}</td>
        </tr></table>
        </td></tr></table>
        </td></tr>

        """;

    private static string LogoBar(int height, string color, int gap) =>
        $"""<td valign="top" style="padding-right:{gap}px;"><table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr><td width="5" height="{height}" bgcolor="{color}" style="width:5px;height:{height}px;background-color:{color};border-radius:1px;font-size:0;line-height:0;">&nbsp;</td></tr></table></td>""";

    private static string KickerBlock(EmailDraft draft, string accent, string soft) =>
        $"""
        <tr><td class="pad" style="padding:30px 44px 0;">
        <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
        <td width="34" height="34" align="center" valign="middle" bgcolor="{soft}" style="width:34px;height:34px;background-color:{soft};border-radius:9px;font-family:{Font};font-size:17px;line-height:34px;font-weight:700;color:{accent};">{Escape(draft.Icon)}</td>
        <td style="padding-left:12px;font-family:{Font};font-size:12px;line-height:16px;font-weight:700;letter-spacing:1.4px;text-transform:uppercase;color:{accent};">{Escape(draft.Kicker)}</td>
        </tr></table>
        </td></tr>

        """;

    private static string Block(string content, int top) =>
        $"""
        <tr><td class="pad" style="padding:{top}px 44px 0;">{content}</td></tr>

        """;

    private static string Paragraph(string text, string color, int weight, int top) =>
        Block(
            $"""<p style="margin:0;font-family:{Font};font-size:16px;line-height:26px;font-weight:{weight};color:{color};">{Escape(text)}</p>""",
            top);

    // The card, board or team the email is about, drawn as a card in a column of the board.
    private static string CardBlock(EmailCardPreview card)
    {
        var actor = card.ActorName is { } name
            ? $"""
              <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin-top:14px;"><tr>
              <td width="26" height="26" align="center" valign="middle" bgcolor="{PrimarySoft}" style="width:26px;height:26px;background-color:{PrimarySoft};border-radius:13px;font-family:{Font};font-size:10px;line-height:26px;font-weight:700;color:{Primary};">{Escape(Initials(name))}</td>
              <td style="padding-left:8px;font-family:{Font};font-size:13px;line-height:18px;color:{Muted};">{Escape(name)}</td>
              </tr></table>
              """
            : string.Empty;

        return Block(
            $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="{Column}" style="background-color:{Column};border-radius:12px;">
            <tr><td style="padding:12px;">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="{Surface}" style="background-color:{Surface};border:1px solid {Line};border-radius:10px;">
            <tr><td style="padding:16px 18px;">
            <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
            <td width="28" height="6" bgcolor="{Primary}" style="width:28px;height:6px;background-color:{Primary};border-radius:3px;font-size:0;line-height:0;">&nbsp;</td>
            <td style="padding-left:8px;font-family:{Font};font-size:11px;line-height:14px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:{Muted};">{Escape(card.Kind)}</td>
            </tr></table>
            <div style="padding-top:10px;font-family:{Font};font-size:17px;line-height:24px;font-weight:700;color:{Text};word-break:break-word;">{Escape(card.Name)}</div>
            {actor}</td></tr>
            </table>
            </td></tr>
            </table>
            """,
            26);
    }

    // The first letter of the first two words, as the app's avatars show them.
    private static string Initials(string name)
    {
        var letters = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0]));
        var initials = string.Concat(letters);
        return initials.Length > 0 ? initials : "?";
    }

    private static string DetailsBlock(IReadOnlyList<EmailDetail> details)
    {
        var rows = new StringBuilder();
        for (var i = 0; i < details.Count; i++)
        {
            var border = i == 0 ? string.Empty : $"border-top:1px solid {Line};";
            rows.Append(
                $"""
                <tr>
                <td valign="top" style="{border}padding:12px 14px 12px 0;font-family:{Font};font-size:13px;line-height:20px;white-space:nowrap;color:{Muted};">{Escape(details[i].Label)}</td>
                <td valign="top" align="right" style="{border}padding:12px 0;font-family:{Font};font-size:14px;line-height:20px;font-weight:600;color:{Text};word-break:break-word;">{Escape(details[i].Value)}</td>
                </tr>

                """);
        }

        return Block(
            $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
            {rows}</table>
            """,
            22);
    }

    // Padding on the link, not the cell, so the whole button is clickable everywhere but Outlook,
    // which still gets a working link and a solid background.
    private static string Button(EmailAction action) =>
        Block(
            $"""
            <table role="presentation" class="button" cellspacing="0" cellpadding="0" border="0"><tr>
            <td align="center" bgcolor="{Primary}" style="background-color:{Primary};border-radius:10px;mso-padding-alt:15px 30px;">
            <a href="{Escape(action.Url)}" target="_blank" rel="noopener" style="display:inline-block;padding:15px 30px;font-family:{Font};font-size:15px;line-height:20px;font-weight:600;color:{Surface};text-decoration:none;border-radius:10px;">{Escape(action.Label)}&nbsp;&rarr;</a>
            </td></tr></table>
            """,
            30);

    private static string LinkFallback(string hint, string url) =>
        Block(
            $"""
            <p style="margin:0;font-family:{Font};font-size:12px;line-height:19px;color:{Muted};">{Escape(hint)}<br>
            <a href="{Escape(url)}" target="_blank" rel="noopener" style="color:{Primary};word-break:break-all;">{Escape(url)}</a></p>
            """,
            18);

    private static string CalloutBlock(EmailDraft draft, string callout, string accent, string soft)
    {
        var warning = draft.Tone is EmailTone.Warning;
        var background = warning ? soft : "#f8fafc";
        var edge = warning ? accent : "#c8cbd0";
        var title = draft.CalloutTitle is { } text
            ? $"""<strong style="display:block;color:{Text};">{Escape(text)}</strong>"""
            : string.Empty;

        return Block(
            $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="{background}" style="background-color:{background};border-left:3px solid {edge};">
            <tr><td style="padding:14px 18px;font-family:{Font};font-size:14px;line-height:22px;color:{Body};">{title}{Escape(callout)}</td></tr>
            </table>
            """,
            28);
    }

    private static string Footer(EmailDraft draft)
    {
        var lines = new StringBuilder();
        foreach (var line in draft.FooterLines)
        {
            lines.Append(
                $"""
                <p style="margin:0 0 8px;font-family:{Font};font-size:12px;line-height:18px;color:{Muted};">{Escape(line)}</p>

                """);
        }

        if (draft.FooterLink is { } link)
        {
            lines.Append(
                $"""
                <p style="margin:0 0 8px;font-family:{Font};font-size:12px;line-height:18px;font-weight:600;"><a href="{Escape(link.Url)}" target="_blank" rel="noopener" style="color:{Primary};">{Escape(link.Label)}</a></p>

                """);
        }

        return $"""
            <tr><td class="pad" align="center" style="padding:28px 44px 8px;text-align:center;">
            <p style="margin:0 0 10px;font-family:{Font};font-size:13px;line-height:18px;font-weight:700;color:{Text};">{Brand}</p>
            {lines}</td></tr>
            """;
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
}
