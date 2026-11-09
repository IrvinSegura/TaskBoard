using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Users;

namespace TaskBoard.Domain.UnitTests.Users;

public class UserEmailPreferencesTests
{
    private readonly User _user = User.Create("ana@example.com", "Ana", "hash");

    [Fact]
    public void WantsEmailFor_ANewUser_WantsEverything()
    {
        foreach (var type in Enum.GetValues<NotificationType>())
        {
            _user.WantsEmailFor(type).ShouldBeTrue();
        }
    }

    [Fact]
    public void MuteEmailNotifications_OnlyMutesThoseTypes()
    {
        _user.MuteEmailNotifications([NotificationType.CommentAdded]);

        _user.WantsEmailFor(NotificationType.CommentAdded).ShouldBeFalse();
        _user.WantsEmailFor(NotificationType.CardAssigned).ShouldBeTrue();
    }

    [Fact]
    public void MuteEmailNotifications_ReplacesWhatWasMutedBefore()
    {
        _user.MuteEmailNotifications([NotificationType.CommentAdded]);

        _user.MuteEmailNotifications([NotificationType.Mentioned, NotificationType.Mentioned, NotificationType.AddedToTeam]);

        _user.MutedEmailNotifications.ShouldBe([NotificationType.AddedToTeam, NotificationType.Mentioned]);
        _user.WantsEmailFor(NotificationType.CommentAdded).ShouldBeTrue();
    }
}
