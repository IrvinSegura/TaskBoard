using Microsoft.Extensions.Logging.Abstractions;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.UnitTests.Notifications;

public class NotificationEmailerTests
{
    private static readonly NotificationEmailFacts Facts = NotificationEmailFacts.Team(Guid.NewGuid(), "Producto");

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailSender _sender = Substitute.For<IEmailSender>();
    private readonly User _user = User.Create("ana@example.com", "Ana", "hash");

    private NotificationEmailer CreateEmailer()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

        var links = Substitute.For<IWebAppLinks>();
        links.To(Arg.Any<string>()).Returns(call => "https://app.example.com" + call.Arg<string>());

        return new NotificationEmailer(_users, new NotificationEmails(links), _sender, NullLogger<NotificationEmailer>.Instance);
    }

    [Fact]
    public async Task SendAsync_ByDefault_SendsTheEmailToTheUser()
    {
        var emailer = CreateEmailer();

        await emailer.SendAsync(_user.Id, NotificationType.AddedToTeam, Facts, CancellationToken.None);

        await _sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "ana@example.com" && m.Subject.Contains("Producto")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenThatTypeIsMuted_SendsNothing()
    {
        var emailer = CreateEmailer();
        _user.MuteEmailNotifications([NotificationType.AddedToTeam]);

        await emailer.SendAsync(_user.Id, NotificationType.AddedToTeam, Facts, CancellationToken.None);

        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task SendAsync_WhenAnotherTypeIsMuted_StillSends()
    {
        var emailer = CreateEmailer();
        _user.MuteEmailNotifications([NotificationType.CommentAdded]);

        await emailer.SendAsync(_user.Id, NotificationType.AddedToTeam, Facts, CancellationToken.None);

        await _sender.ReceivedWithAnyArgs(1).SendAsync(default!, default);
    }

    [Fact]
    public async Task SendAsync_UnknownUser_SendsNothing()
    {
        var emailer = CreateEmailer();

        await emailer.SendAsync(Guid.NewGuid(), NotificationType.AddedToTeam, Facts, CancellationToken.None);

        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task SendAsync_WhenSendingFails_DoesNotFailTheNotification()
    {
        var emailer = CreateEmailer();
        _sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Mail is down."));

        await Should.NotThrowAsync(() =>
            emailer.SendAsync(_user.Id, NotificationType.AddedToTeam, Facts, CancellationToken.None));
    }
}
