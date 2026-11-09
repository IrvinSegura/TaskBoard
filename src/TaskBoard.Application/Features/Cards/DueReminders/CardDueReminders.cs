using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Cards.DueReminders;

internal sealed class CardDueReminders(
    ICardRepository cards,
    IUnitOfWork unitOfWork,
    NotificationPublisher publisher,
    TimeProvider timeProvider)
    : ICardDueReminders
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    public async Task<int> SendAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var due = await cards.GetDueForReminderAsync(now, now + Window, cancellationToken);
        if (due.Count == 0)
        {
            return 0;
        }

        // Marked before anything is sent: a crash midway skips a reminder rather than repeating it.
        foreach (var card in due)
        {
            card.MarkDueReminderSent();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sent = 0;
        foreach (var card in due)
        {
            foreach (var assignment in card.Assignments)
            {
                await publisher.PublishAsync(
                    assignment.UserId,
                    NotificationType.CardDueSoon,
                    $"\"{card.Title}\" is due soon.",
                    card.Id,
                    NotificationEmailFacts.Card(card.BoardId, card.Id, card.Title),
                    cancellationToken);
                sent++;
            }
        }

        return sent;
    }
}
