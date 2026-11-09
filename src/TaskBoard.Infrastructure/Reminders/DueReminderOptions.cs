namespace TaskBoard.Infrastructure.Reminders;

public sealed class DueReminderOptions
{
    public const string SectionName = "DueReminders";

    // How often due cards are looked for; zero or less turns the reminders off.
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(15);
}
