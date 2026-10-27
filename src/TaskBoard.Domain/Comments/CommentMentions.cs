using System.Text.RegularExpressions;

namespace TaskBoard.Domain.Comments;

/// <summary>
/// Mentions are written in the comment body as <c>@[Display Name](user:id)</c>; the client turns them into chips.
/// </summary>
public static partial class CommentMentions
{
    public static IReadOnlyList<Guid> UserIds(string body) =>
        MentionPattern().Matches(body)
            .Select(match => Guid.Parse(match.Groups["id"].Value))
            .Distinct()
            .ToList();

    // The body as people read it: every mention becomes "@Display Name".
    public static string ToPlainText(string body) =>
        MentionPattern().Replace(body, match => "@" + match.Groups["name"].Value);

    [GeneratedRegex(@"@\[(?<name>[^\]\r\n]{1,100})\]\(user:(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\)")]
    private static partial Regex MentionPattern();
}
