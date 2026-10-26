using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Attachments.Events;

namespace TaskBoard.Domain.UnitTests.Attachments;

public class AttachmentTests
{
    private static Attachment CreateAttachment(Guid uploadedById) =>
        Attachment.Create(Guid.NewGuid(), Guid.NewGuid(), uploadedById, "spec.pdf", "application/pdf", 2048, "key-123");

    [Fact]
    public void Delete_ByUploader_RaisesAttachmentDeletedEventWithStorageKey()
    {
        var uploaderId = Guid.NewGuid();
        var attachment = CreateAttachment(uploaderId);

        var result = attachment.Delete(uploaderId, actorIsBoardAdmin: false);

        result.IsSuccess.ShouldBeTrue();
        attachment.DomainEvents.OfType<AttachmentDeletedEvent>().ShouldHaveSingleItem().StorageKey.ShouldBe("key-123");
    }

    [Fact]
    public void Delete_ByAnotherNonAdminUser_ReturnsCannotDelete()
    {
        var attachment = CreateAttachment(Guid.NewGuid());

        var result = attachment.Delete(Guid.NewGuid(), actorIsBoardAdmin: false);

        result.Error.ShouldBe(AttachmentErrors.CannotDelete);
        attachment.DomainEvents.OfType<AttachmentDeletedEvent>().ShouldBeEmpty();
    }
}
