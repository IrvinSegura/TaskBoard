using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Comments;

namespace TaskBoard.Domain.UnitTests.Comments;

public class CommentMentionsTests
{
    private static readonly Guid Laura = Guid.Parse("01a17ea5-0000-7000-8000-000000000001");
    private static readonly Guid Diego = Guid.Parse("01a17ea5-0000-7000-8000-000000000002");

    [Fact]
    public void UserIds_FindsEveryMentionOnce()
    {
        var body = $"@[Laura Méndez](user:{Laura}) y @[Diego Torres](user:{Diego}), ¿lo vemos? @[Laura Méndez](user:{Laura})";

        CommentMentions.UserIds(body).ShouldBe([Laura, Diego]);
    }

    [Fact]
    public void UserIds_IgnoresPlainAtSignsAndBrokenTokens()
    {
        CommentMentions.UserIds("escribe a soporte@empresa.com o @[Laura](user:no-es-un-id)").ShouldBeEmpty();
    }

    [Fact]
    public void ToPlainText_ShowsMentionsAsNames()
    {
        CommentMentions.ToPlainText($"Hola @[Laura Méndez](user:{Laura})!").ShouldBe("Hola @Laura Méndez!");
    }

    [Fact]
    public void ActivityEntry_TruncatesLongDetails()
    {
        var entry = ActivityEntry.Create(
            Guid.NewGuid(), null, null, ActivityType.CommentAdded, "Card", new string('a', 500), DateTimeOffset.UtcNow);

        entry.Detail!.Length.ShouldBe(ActivityEntry.DetailMaxLength);
        entry.Detail.ShouldEndWith("…");
    }
}
