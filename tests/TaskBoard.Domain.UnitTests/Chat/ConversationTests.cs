using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Domain.UnitTests.Chat;

public class ConversationTests
{
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _beto = Guid.NewGuid();
    private readonly Guid _carla = Guid.NewGuid();

    [Fact]
    public void StartDirect_WithYourself_Fails()
    {
        Conversation.StartDirect(_ana, _ana).Error.ShouldBe(ChatErrors.CannotMessageYourself);
    }

    [Fact]
    public void StartDirect_GivesThePairTheSameKeyWhoeverStartsIt()
    {
        var started = Conversation.StartDirect(_ana, _beto).Value;

        started.Kind.ShouldBe(ConversationKind.Direct);
        started.DirectKey.ShouldBe(ChatRules.DirectKey(_beto, _ana));
        started.Participants.Select(p => p.UserId).ShouldBe([_ana, _beto], ignoreOrder: true);
    }

    [Fact]
    public void StartGroup_AddsTheCreatorOnce()
    {
        var group = Conversation.StartGroup(_ana, "  Lanzamiento  ", [_beto, _ana, _beto]).Value;

        group.Title.ShouldBe("Lanzamiento");
        group.Participants.Select(p => p.UserId).ShouldBe([_beto, _ana], ignoreOrder: true);
    }

    [Fact]
    public void StartGroup_NeedsSomeoneElse()
    {
        Conversation.StartGroup(_ana, "Solo", [_ana]).Error.ShouldBe(ChatErrors.GroupNeedsParticipants);
    }

    [Fact]
    public void StartGroup_RejectsTooManyPeople()
    {
        var people = Enumerable.Range(0, ChatRules.MaxGroupParticipants).Select(_ => Guid.NewGuid()).ToList();

        Conversation.StartGroup(_ana, "Todos", people).Error.ShouldBe(ChatErrors.TooManyParticipants);
    }

    [Fact]
    public void AddParticipants_SkipsPeopleAlreadyInAndTellsEveryone()
    {
        var group = Conversation.StartGroup(_ana, "Equipo", [_beto]).Value;

        group.AddParticipants(_beto, [_ana, _carla]).IsSuccess.ShouldBeTrue();

        group.Participants.Count.ShouldBe(3);
        group.DomainEvents.OfType<ConversationUpdatedEvent>().ShouldHaveSingleItem()
            .UserIds.ShouldBe([_ana, _beto, _carla], ignoreOrder: true);
    }

    [Fact]
    public void AddParticipants_ByAnOutsider_IsNotFound()
    {
        var group = Conversation.StartGroup(_ana, "Equipo", [_beto]).Value;

        group.AddParticipants(_carla, [_carla]).Error.Code.ShouldBe("Chat.NotFound");
    }

    [Fact]
    public void Leave_RemovesTheParticipantAndStillTellsThem()
    {
        var group = Conversation.StartGroup(_ana, "Equipo", [_beto, _carla]).Value;

        group.Leave(_carla).IsSuccess.ShouldBeTrue();

        group.HasParticipant(_carla).ShouldBeFalse();
        group.DomainEvents.OfType<ConversationUpdatedEvent>().ShouldHaveSingleItem().UserIds.ShouldContain(_carla);
    }

    [Fact]
    public void DirectConversations_CannotBeRenamedOrLeft()
    {
        var direct = Conversation.StartDirect(_ana, _beto).Value;

        direct.Rename(_ana, "Nuevo").Error.ShouldBe(ChatErrors.NotAGroup);
        direct.Leave(_ana).Error.ShouldBe(ChatErrors.NotAGroup);
        direct.AddParticipants(_ana, [_carla]).Error.ShouldBe(ChatErrors.NotAGroup);
    }

    [Fact]
    public void ReadMarker_NeverMovesBack()
    {
        var now = DateTimeOffset.UtcNow;
        var marker = ConversationReadMarker.Create(Guid.NewGuid(), _ana, now);

        marker.MarkRead(now.AddMinutes(-5));

        marker.LastReadAt.ShouldBe(now);
    }
}
