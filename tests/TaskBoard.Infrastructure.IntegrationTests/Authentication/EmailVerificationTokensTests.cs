using Microsoft.Extensions.Options;
using TaskBoard.Infrastructure.Authentication;

namespace TaskBoard.Infrastructure.IntegrationTests.Authentication;

public class EmailVerificationTokensTests
{
    private const string SigningKey = "email-verification-tests-key-0123456789abcdef";

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly Clock _clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

    private EmailVerificationTokens Tokens(string key = SigningKey) =>
        new(Options.Create(new JwtOptions { SigningKey = key }), _clock);

    [Fact]
    public void Read_ATokenItMade_GivesBackTheUser()
    {
        var userId = Guid.NewGuid();

        Tokens().Read(Tokens().Create(userId)).ShouldBe(userId);
    }

    [Fact]
    public void Read_AfterItExpires_GivesNothing()
    {
        var token = Tokens().Create(Guid.NewGuid());

        _clock.Now += Tokens().ValidFor + TimeSpan.FromSeconds(1);

        Tokens().Read(token).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a token")]
    [InlineData("AAAA")]
    public void Read_Garbage_GivesNothing(string token)
    {
        Tokens().Read(token).ShouldBeNull();
    }

    [Fact]
    public void Read_ATamperedOrForeignToken_GivesNothing()
    {
        var token = Tokens().Create(Guid.NewGuid());
        var tampered = (token[0] == 'A' ? "B" : "A") + token[1..];

        Tokens().Read(tampered).ShouldBeNull();
        Tokens("another-signing-key-0123456789abcdef-xyz").Read(token).ShouldBeNull();
    }
}
