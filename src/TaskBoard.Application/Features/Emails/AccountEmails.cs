using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Application.Features.Emails;

/// <summary>
/// Emails about the account's security. They are always sent: the user may need to act on them.
/// </summary>
internal sealed class AccountEmails(IWebAppLinks links)
{
    private const string SecurityFooter = "TaskBoard nunca te pedirá tu contraseña por correo.";

    public EmailMessage ResetPassword(string to, string displayName, string token, TimeSpan validFor)
    {
        // In the fragment, which browsers never send anywhere: the token stays out of server logs and Referer headers.
        var url = links.To($"/reset-password#token={Uri.EscapeDataString(token)}");

        return EmailLayout.Message(to, new EmailDraft
        {
            Tone = EmailTone.Info,
            Icon = "✱",
            Kicker = "Seguridad de la cuenta",
            Subject = "Restablece tu contraseña de TaskBoard",
            Preheader = $"El enlace vence en {Duration(validFor)}.",
            Title = "Restablece tu contraseña",
            Greeting = $"Hola {displayName},",
            Paragraphs =
            [
                "Recibimos una solicitud para restablecer la contraseña de tu cuenta.",
                $"El enlace vence en {Duration(validFor)} y solo funciona una vez."
            ],
            Action = new EmailAction("Elegir una contraseña nueva", url),
            LinkHint = "Si el botón no funciona, copia este enlace en tu navegador:",
            CalloutTitle = "¿No fuiste tú?",
            Callout = "Ignora este correo: tu contraseña sigue igual mientras nadie use el enlace.",
            FooterLines = [SecurityFooter]
        });
    }

    public EmailMessage PasswordChanged(string to, string displayName) =>
        EmailLayout.Message(to, new EmailDraft
        {
            Tone = EmailTone.Warning,
            Icon = "!",
            Kicker = "Aviso de seguridad",
            Subject = "Tu contraseña de TaskBoard cambió",
            Preheader = "Si no fuiste tú, recupera tu cuenta cuanto antes.",
            Title = "Tu contraseña cambió",
            Greeting = $"Hola {displayName},",
            Paragraphs =
            [
                "La contraseña de tu cuenta se acaba de cambiar y se cerraron todas tus sesiones.",
                "Si fuiste tú, no tienes que hacer nada más."
            ],
            Action = new EmailAction("Recuperar mi cuenta", links.To("/forgot-password")),
            CalloutTitle = "¿No fuiste tú?",
            Callout = "Alguien más conoce tu contraseña. Pide un enlace para restablecerla cuanto antes.",
            FooterLines = [SecurityFooter]
        });

    // Sent once, right after signing up.
    public EmailMessage Welcome(string to, string displayName, string token, TimeSpan validFor) =>
        EmailLayout.Message(to, new EmailDraft
        {
            Tone = EmailTone.Info,
            Icon = "✦",
            Kicker = "Bienvenida",
            Subject = "Te damos la bienvenida a TaskBoard",
            Preheader = "Confirma tu correo para terminar de configurar tu cuenta.",
            Title = "Tu cuenta está lista",
            Greeting = $"Hola {displayName},",
            Paragraphs =
            [
                "Ya puedes crear equipos y tableros, y organizar el trabajo en tarjetas.",
                "Confirma que este correo es tuyo: aquí te avisamos de tus tarjetas y aquí llega el enlace si olvidas tu contraseña.",
                $"El enlace vence en {Days(validFor)}."
            ],
            Action = new EmailAction("Confirmar mi correo", VerifyUrl(token)),
            LinkHint = "Si el botón no funciona, copia este enlace en tu navegador:",
            CalloutTitle = "¿No creaste esta cuenta?",
            Callout = "Alguien escribió tu correo al registrarse. Ignora este mensaje y no abras el enlace.",
            FooterLines = [SecurityFooter]
        });

    // When the owner asks again from their account.
    public EmailMessage VerifyEmail(string to, string displayName, string token, TimeSpan validFor) =>
        EmailLayout.Message(to, new EmailDraft
        {
            Tone = EmailTone.Info,
            Icon = "@",
            Kicker = "Confirmación de correo",
            Subject = "Confirma tu correo de TaskBoard",
            Preheader = $"El enlace vence en {Days(validFor)}.",
            Title = "Confirma tu correo",
            Greeting = $"Hola {displayName},",
            Paragraphs =
            [
                "Pediste un enlace nuevo para confirmar el correo de tu cuenta.",
                $"El enlace vence en {Days(validFor)}."
            ],
            Action = new EmailAction("Confirmar mi correo", VerifyUrl(token)),
            LinkHint = "Si el botón no funciona, copia este enlace en tu navegador:",
            CalloutTitle = "¿No lo pediste?",
            Callout = "Ignora este correo: tu cuenta sigue igual.",
            FooterLines = [SecurityFooter]
        });

    // In the fragment, like the reset link, so the token stays out of server logs.
    private string VerifyUrl(string token) => links.To($"/verify-email#token={Uri.EscapeDataString(token)}");

    private static string Days(TimeSpan duration) => duration.Days == 1 ? "1 día" : $"{duration.Days} días";

    private static string Duration(TimeSpan duration)
    {
        var minutes = (int)Math.Round(duration.TotalMinutes);
        return minutes switch
        {
            60 => "1 hora",
            > 60 when minutes % 60 == 0 => $"{minutes / 60} horas",
            1 => "1 minuto",
            _ => $"{minutes} minutos"
        };
    }
}
