using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Domain.UnitTests.Chat;

public class ChatMessageTests
{
    private readonly Guid _conversationId = Guid.NewGuid();
    private readonly Guid _senderId = Guid.NewGuid();

    private static ChatFile Photo(long size = 1024) => new("foto.png", "image/png", size, Guid.NewGuid().ToString("N"));

    [Fact]
    public void Send_TrimsTheTextAndRaisesChatMessageSentEvent()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "  Hola  ", []).Value;

        message.Body.ShouldBe("Hola");
        message.DomainEvents.OfType<ChatMessageSentEvent>().ShouldHaveSingleItem().SenderId.ShouldBe(_senderId);
    }

    [Fact]
    public void Send_WithOnlyFiles_HasNoText()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "   ", [Photo()]).Value;

        message.Body.ShouldBeNull();
        message.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("foto.png");
    }

    [Fact]
    public void Send_KeepsTheUploadOrder()
    {
        var files = new[] { Photo() with { FileName = "b.png" }, Photo() with { FileName = "a.png" } };

        var message = ChatMessage.Send(_conversationId, _senderId, null, files).Value;

        message.Attachments.OrderBy(a => a.Position).Select(a => a.FileName).ShouldBe(["b.png", "a.png"]);
    }

    [Fact]
    public void Send_WithNothing_Fails()
    {
        ChatMessage.Send(_conversationId, _senderId, " ", []).Error.ShouldBe(ChatErrors.EmptyMessage);
    }

    [Fact]
    public void Send_RejectsTooManyOrTooLargeFiles()
    {
        var tooMany = Enumerable.Range(0, ChatRules.MaxAttachmentsPerMessage + 1).Select(_ => Photo()).ToList();
        ChatMessage.Send(_conversationId, _senderId, null, tooMany).Error.ShouldBe(ChatErrors.TooManyAttachments);

        var tooLarge = Enumerable.Range(0, 3).Select(_ => Photo(ChatRules.MaxFileSizeInBytes)).ToList();
        ChatMessage.Send(_conversationId, _senderId, null, tooLarge).Error.ShouldBe(ChatErrors.MessageTooLarge);
    }

    [Fact]
    public void Edit_BySender_ChangesTheTextAndMarksItEdited()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "Borrador", []).Value;

        message.Edit(_senderId, "Final").IsSuccess.ShouldBeTrue();

        message.Body.ShouldBe("Final");
        message.IsEdited.ShouldBeTrue();
        message.DomainEvents.OfType<ChatMessageEditedEvent>().ShouldHaveSingleItem().Body.ShouldBe("Final");
    }

    [Fact]
    public void Edit_WithTheSameText_ChangesNothing()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "Igual", []).Value;

        message.Edit(_senderId, " Igual ").IsSuccess.ShouldBeTrue();

        message.IsEdited.ShouldBeFalse();
        message.DomainEvents.OfType<ChatMessageEditedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Edit_CanClearTheTextOnlyWhileFilesRemain()
    {
        var textOnly = ChatMessage.Send(_conversationId, _senderId, "Texto", []).Value;
        textOnly.Edit(_senderId, "").Error.ShouldBe(ChatErrors.EmptyMessage);

        var withPhoto = ChatMessage.Send(_conversationId, _senderId, "Mira", [Photo()]).Value;
        withPhoto.Edit(_senderId, "").IsSuccess.ShouldBeTrue();
        withPhoto.Body.ShouldBeNull();
    }

    [Fact]
    public void Edit_BySomeoneElse_IsForbidden()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "Mío", []).Value;

        message.Edit(Guid.NewGuid(), "Tuyo").Error.ShouldBe(ChatErrors.NotSender);
    }

    [Fact]
    public void Delete_DropsTextAndFilesAndHandsTheStorageKeysToTheEvent()
    {
        var photo = Photo();
        var message = ChatMessage.Send(_conversationId, _senderId, "Adiós", [photo]).Value;

        message.Delete(_senderId).IsSuccess.ShouldBeTrue();

        message.IsDeleted.ShouldBeTrue();
        message.Body.ShouldBeNull();
        message.Attachments.ShouldBeEmpty();
        message.DomainEvents.OfType<ChatMessageDeletedEvent>().ShouldHaveSingleItem()
            .StorageKeys.ShouldBe([photo.StorageKey]);
    }

    [Fact]
    public void DeletedMessages_CannotBeEditedOrDeletedAgain()
    {
        var message = ChatMessage.Send(_conversationId, _senderId, "Adiós", []).Value;
        message.Delete(_senderId);

        message.Edit(_senderId, "Hola de nuevo").Error.ShouldBe(ChatErrors.MessageDeleted);
        message.Delete(_senderId).Error.ShouldBe(ChatErrors.MessageDeleted);
    }

    [Fact]
    public void DirectKey_IsTheSameInBothDirections()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        ChatRules.DirectKey(a, b).ShouldBe(ChatRules.DirectKey(b, a));
    }
}
