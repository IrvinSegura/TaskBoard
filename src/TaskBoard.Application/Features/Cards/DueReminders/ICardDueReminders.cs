namespace TaskBoard.Application.Features.Cards.DueReminders;

/// <summary>
/// Reminds the assignees of each card that is due within a day. Run periodically by the infrastructure.
/// </summary>
public interface ICardDueReminders
{
    // Returns how many reminders went out.
    Task<int> SendAsync(CancellationToken cancellationToken);
}
