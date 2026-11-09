using TaskBoard.Domain.Users;

namespace TaskBoard.Domain.UnitTests.Users;

public class PasswordResetTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActive_BeforeExpiring_IsTrue()
    {
        PasswordResetToken.Create(Guid.NewGuid(), "hash", Now.AddMinutes(1)).IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_OnceExpired_IsFalse()
    {
        PasswordResetToken.Create(Guid.NewGuid(), "hash", Now).IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void Consume_EndsTheTokenAndKeepsTheFirstTime()
    {
        var token = PasswordResetToken.Create(Guid.NewGuid(), "hash", Now.AddHours(1));

        token.Consume(Now);
        token.Consume(Now.AddMinutes(5));

        token.IsActive(Now).ShouldBeFalse();
        token.ConsumedAt.ShouldBe(Now);
    }
}
