using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Emails;

/// <summary>
/// The email copy of each notification. People can turn these off, so all of them link to that setting.
/// </summary>
internal sealed class NotificationEmails(IWebAppLinks links)
{
    private sealed record Wording(
        string Icon,
        string Kicker,
        string Subject,
        string Title,
        string Sentence,
        string ResourceLabel,
        string ActionLabel,
        EmailTone Tone = EmailTone.Info);

    public EmailMessage Create(NotificationType type, string to, string displayName, NotificationEmailFacts facts)
    {
        var wording = WordingFor(type, facts.ResourceName, facts.ActorName ?? "Alguien", facts.Detail ?? "");

        return EmailLayout.Message(to, new EmailDraft
        {
            Tone = wording.Tone,
            Icon = wording.Icon,
            Kicker = wording.Kicker,
            Subject = wording.Subject,
            Preheader = wording.Sentence,
            Title = wording.Title,
            Greeting = $"Hola {displayName},",
            Paragraphs = [wording.Sentence],
            Card = new EmailCardPreview(wording.ResourceLabel, facts.ResourceName, facts.ActorName),
            Action = new EmailAction(wording.ActionLabel, links.To(facts.Path)),
            FooterLines = ["Recibes este correo porque tienes activado este aviso en tu cuenta de TaskBoard."],
            FooterLink = new EmailAction("Elegir qué correos recibo", links.To("/account"))
        });
    }

    private static Wording WordingFor(NotificationType type, string name, string actor, string detail) => type switch
    {
        NotificationType.CardAssigned => new Wording(
            "✓",
            "Asignación",
            $"Te asignaron la tarjeta «{name}»",
            "Tienes una tarjeta nueva",
            $"Te asignaron la tarjeta «{name}».",
            "Tarjeta",
            "Ver tarjeta"),
        NotificationType.Mentioned => new Wording(
            "@",
            "Mención",
            $"{actor} te mencionó en «{name}»",
            "Te mencionaron en un comentario",
            $"{actor} te mencionó en un comentario de la tarjeta «{name}».",
            "Tarjeta",
            "Ver comentario"),
        NotificationType.CommentAdded => new Wording(
            "#",
            "Comentario",
            $"{actor} comentó en «{name}»",
            "Hay un comentario nuevo",
            $"{actor} comentó en la tarjeta «{name}», que tienes asignada.",
            "Tarjeta",
            "Ver comentario"),
        NotificationType.AddedToBoard => new Wording(
            "+",
            "Tablero nuevo",
            $"Te agregaron al tablero «{name}»",
            "Tienes un tablero nuevo",
            $"Te agregaron al tablero «{name}». Ya puedes ver sus tarjetas.",
            "Tablero",
            "Ver tablero"),
        NotificationType.AddedToTeam => new Wording(
            "+",
            "Equipo nuevo",
            $"Te agregaron al equipo «{name}»",
            "Tienes un equipo nuevo",
            $"Te agregaron al equipo «{name}». Ya puedes ver sus tableros.",
            "Equipo",
            "Ver equipo"),
        NotificationType.RemovedFromBoard => new Wording(
            "−",
            "Acceso",
            $"Ya no estás en el tablero «{name}»",
            "Te quitaron de un tablero",
            $"{actor} te quitó del tablero «{name}». Ya no verás sus tarjetas.",
            "Tablero",
            "Ver mis tableros"),
        NotificationType.RemovedFromTeam => new Wording(
            "−",
            "Acceso",
            $"Ya no estás en el equipo «{name}»",
            "Te quitaron de un equipo",
            $"{actor} te quitó del equipo «{name}». Ya no verás sus tableros.",
            "Equipo",
            "Ver mis equipos"),
        NotificationType.TeamRoleChanged => new Wording(
            "⇅",
            "Cambio de rol",
            $"Ahora eres {detail} del equipo «{name}»",
            "Cambió tu rol en un equipo",
            $"{actor} cambió tu rol en el equipo «{name}». Ahora eres {detail}.",
            "Equipo",
            "Ver equipo"),
        NotificationType.CardDueSoon => new Wording(
            "!",
            "Recordatorio",
            $"La tarjeta «{name}» vence pronto",
            "Una tarjeta vence pronto",
            $"La tarjeta «{name}», que tienes asignada, vence en menos de 24 horas.",
            "Tarjeta",
            "Ver tarjeta",
            EmailTone.Warning),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "This notification has no email.")
    };
}
