using TaskBoard.Domain.Chat.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

/// <summary>
/// Where chat messages are posted. Direct and group conversations store who takes part; board and team
/// conversations belong to their board or team and take their members from it, so access always follows
/// the board and team membership rules.
/// </summary>
public sealed class Conversation : AggregateRoot
{
    private readonly List<ConversationParticipant> _participants = [];

    private Conversation()
    {
    }

    public ConversationKind Kind { get; private set; }

    // Only groups have one; the others show the other person, the board or the team.
    public string? Title { get; private set; }

    public Guid? BoardId { get; private set; }

    public Guid? TeamId { get; private set; }

    // Set on direct conversations only (see ChatRules.DirectKey).
    public string? DirectKey { get; private set; }

    public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();

    public static Result<Conversation> StartDirect(Guid userId, Guid otherUserId)
    {
        if (userId == otherUserId)
        {
            return ChatErrors.CannotMessageYourself;
        }

        var conversation = new Conversation
        {
            Kind = ConversationKind.Direct,
            DirectKey = ChatRules.DirectKey(userId, otherUserId)
        };
        conversation._participants.Add(new ConversationParticipant(conversation.Id, userId));
        conversation._participants.Add(new ConversationParticipant(conversation.Id, otherUserId));
        return conversation;
    }

    public static Result<Conversation> StartGroup(Guid creatorId, string title, IReadOnlyCollection<Guid> participantIds)
    {
        var userIds = participantIds.Append(creatorId).Distinct().ToList();
        if (userIds.Count < 2)
        {
            return ChatErrors.GroupNeedsParticipants;
        }

        if (userIds.Count > ChatRules.MaxGroupParticipants)
        {
            return ChatErrors.TooManyParticipants;
        }

        var conversation = new Conversation { Kind = ConversationKind.Group, Title = title.Trim() };
        conversation._participants.AddRange(userIds.Select(id => new ConversationParticipant(conversation.Id, id)));
        return conversation;
    }

    public static Conversation ForBoard(Guid boardId) => new() { Kind = ConversationKind.Board, BoardId = boardId };

    public static Conversation ForTeam(Guid teamId) => new() { Kind = ConversationKind.Team, TeamId = teamId };

    public bool HasParticipant(Guid userId) => _participants.Any(p => p.UserId == userId);

    public Result Rename(Guid actorId, string title)
    {
        var check = EnsureGroupParticipant(actorId);
        if (check.IsFailure)
        {
            return check;
        }

        Title = title.Trim();
        RaiseUpdated([]);
        return Result.Success();
    }

    // Any participant can bring people in. People already in the group are skipped.
    public Result AddParticipants(Guid actorId, IReadOnlyCollection<Guid> userIds)
    {
        var check = EnsureGroupParticipant(actorId);
        if (check.IsFailure)
        {
            return check;
        }

        var newUserIds = userIds.Distinct().Where(id => !HasParticipant(id)).ToList();
        if (_participants.Count + newUserIds.Count > ChatRules.MaxGroupParticipants)
        {
            return ChatErrors.TooManyParticipants;
        }

        if (newUserIds.Count == 0)
        {
            return Result.Success();
        }

        _participants.AddRange(newUserIds.Select(id => new ConversationParticipant(Id, id)));
        RaiseUpdated([]);
        return Result.Success();
    }

    // The group stays even when its last participant leaves; nobody can open it any more.
    public Result Leave(Guid userId)
    {
        var check = EnsureGroupParticipant(userId);
        if (check.IsFailure)
        {
            return check;
        }

        _participants.RemoveAll(p => p.UserId == userId);
        RaiseUpdated([userId]);
        return Result.Success();
    }

    private Result EnsureGroupParticipant(Guid userId)
    {
        if (!HasParticipant(userId))
        {
            return ChatErrors.NotFound(Id);
        }

        return Kind == ConversationKind.Group ? Result.Success() : ChatErrors.NotAGroup;
    }

    private void RaiseUpdated(IEnumerable<Guid> formerParticipantIds) =>
        Raise(new ConversationUpdatedEvent(Id, [.. _participants.Select(p => p.UserId), .. formerParticipantIds]));
}
