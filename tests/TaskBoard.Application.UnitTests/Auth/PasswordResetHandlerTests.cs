using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Auth;
using TaskBoard.Application.Features.Auth.ForgotPassword;
using TaskBoard.Application.Features.Auth.ResetPassword;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.UnitTests.Auth;

public class PasswordResetHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _resetTokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IPasswordResetTokenProvider _tokenProvider = Substitute.For<IPasswordResetTokenProvider>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _sender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AccountEmails _emails;
    private readonly User _user = User.Create("ana@example.com", "Ana", "old-hash");

    public PasswordResetHandlerTests()
    {
        var links = Substitute.For<IWebAppLinks>();
        links.To(Arg.Any<string>()).Returns(call => "https://app.example.com" + call.Arg<string>());
        _emails = new AccountEmails(links);

        _users.GetByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _tokenProvider.Hash("the-token").Returns("the-hash");
        _tokenProvider.Create().Returns(new GeneratedPasswordResetToken(
            "new-token", "new-hash", DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromHours(1)));
        _passwordHasher.Hash("new-password").Returns("new-hash");
    }

    private ForgotPasswordCommandHandler CreateForgotHandler() =>
        new(_users, _resetTokens, _tokenProvider, _emails, _sender, _unitOfWork, TimeProvider.System);

    private ResetPasswordCommandHandler CreateResetHandler() =>
        new(_resetTokens, _tokenProvider, _users, _passwordHasher,
            new AuthTokenService(Substitute.For<ITokenProvider>(), _refreshTokens, TimeProvider.System),
            _emails, _sender, _unitOfWork, TimeProvider.System);

    private PasswordResetToken GivenStoredToken(DateTimeOffset? expiresAt = null)
    {
        var token = PasswordResetToken.Create(_user.Id, "the-hash", expiresAt ?? DateTimeOffset.UtcNow.AddHours(1));
        _resetTokens.GetByHashAsync("the-hash", Arg.Any<CancellationToken>()).Returns(token);
        _resetTokens.GetActiveForUserAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([token]);
        return token;
    }

    [Fact]
    public async Task Forgot_UnknownEmail_SucceedsWithoutSendingAnything()
    {
        var result = await CreateForgotHandler().Handle(new ForgotPasswordCommand("nobody@example.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _resetTokens.DidNotReceiveWithAnyArgs().Add(default!);
        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Forgot_KnownEmail_StoresOnlyTheHashAndEmailsThePlainToken()
    {
        var result = await CreateForgotHandler().Handle(new ForgotPasswordCommand(_user.Email), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _resetTokens.Received(1).Add(Arg.Is<PasswordResetToken>(t => t.UserId == _user.Id && t.TokenHash == "new-hash"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == _user.Email && m.TextBody.Contains("#token=new-token")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Forgot_WithAnOlderActiveLink_ReplacesIt()
    {
        var older = GivenStoredToken();

        await CreateForgotHandler().Handle(new ForgotPasswordCommand(_user.Email), CancellationToken.None);

        older.IsActive(DateTimeOffset.UtcNow).ShouldBeFalse();
        _resetTokens.Received(1).Add(Arg.Any<PasswordResetToken>());
    }

    [Fact]
    public async Task Forgot_WhenAParallelRequestWon_SucceedsWithoutSendingASecondEmail()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DbUpdateConcurrencyException());

        var result = await CreateForgotHandler().Handle(new ForgotPasswordCommand(_user.Email), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Reset_WithAValidToken_ChangesThePasswordSpendsTheTokenAndWarnsTheUser()
    {
        var token = GivenStoredToken();

        var result = await CreateResetHandler().Handle(new ResetPasswordCommand("the-token", "new-password"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _user.PasswordHash.ShouldBe("new-hash");
        token.IsActive(DateTimeOffset.UtcNow).ShouldBeFalse();
        await _refreshTokens.Received(1).GetActiveForUserAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == _user.Email && m.Subject.Contains("cambió")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reset_UnknownToken_ReturnsInvalidTokenAndChangesNothing()
    {
        var result = await CreateResetHandler().Handle(new ResetPasswordCommand("the-token", "new-password"), CancellationToken.None);

        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
        _user.PasswordHash.ShouldBe("old-hash");
    }

    [Fact]
    public async Task Reset_ExpiredToken_ReturnsInvalidTokenAndChangesNothing()
    {
        GivenStoredToken(expiresAt: DateTimeOffset.UtcNow.AddSeconds(-1));

        var result = await CreateResetHandler().Handle(new ResetPasswordCommand("the-token", "new-password"), CancellationToken.None);

        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
        _user.PasswordHash.ShouldBe("old-hash");
    }

    [Fact]
    public async Task Reset_SpentToken_ReturnsInvalidTokenAndChangesNothing()
    {
        GivenStoredToken().Consume(DateTimeOffset.UtcNow);

        var result = await CreateResetHandler().Handle(new ResetPasswordCommand("the-token", "new-password"), CancellationToken.None);

        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
        _user.PasswordHash.ShouldBe("old-hash");
    }

    [Fact]
    public async Task Reset_WhenAParallelRequestUsedTheTokenFirst_ReturnsInvalidTokenWithoutWarning()
    {
        GivenStoredToken();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DbUpdateConcurrencyException());

        var result = await CreateResetHandler().Handle(new ResetPasswordCommand("the-token", "new-password"), CancellationToken.None);

        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }
}
