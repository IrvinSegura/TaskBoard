using TaskBoard.Domain.Users;
using TaskBoard.Domain.Users.Events;

namespace TaskBoard.Domain.UnitTests.Users;

public class UserEmailVerificationTests
{
    [Fact]
    public void Create_StartsUnverifiedAndAnnouncesTheUser()
    {
        var user = User.Create("ana@example.com", "Ana", "hash");

        user.IsEmailVerified.ShouldBeFalse();
        user.DomainEvents.OfType<UserRegisteredEvent>().ShouldHaveSingleItem().UserId.ShouldBe(user.Id);
    }

    [Fact]
    public void VerifyEmail_Again_KeepsTheFirstDate()
    {
        var user = User.Create("ana@example.com", "Ana", "hash");
        var first = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        user.VerifyEmail(first);
        user.VerifyEmail(first.AddDays(1));

        user.IsEmailVerified.ShouldBeTrue();
        user.EmailVerifiedAt.ShouldBe(first);
    }
}
