using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Comments;

public class CommentAndAttachmentEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task DeleteComment_OnlyAuthorOrBoardAdminCanDelete()
    {
        var (admin, author, otherEditor, cardId) = await CreateBoardWithEditorsAsync();
        var commentId = await ApiDriver.ReadIdAsync(
            await author.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body = "Mine" }));

        var byOtherEditor = await otherEditor.Client.DeleteAsync($"/comments/{commentId}");
        byOtherEditor.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var byAdmin = await admin.Client.DeleteAsync($"/comments/{commentId}");
        byAdmin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var comments = await admin.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}/comments");
        comments.GetProperty("items").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task EditComment_ByAnotherUser_IsForbidden()
    {
        var (_, author, otherEditor, cardId) = await CreateBoardWithEditorsAsync();
        var commentId = await ApiDriver.ReadIdAsync(
            await author.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body = "Mine" }));

        var edit = await otherEditor.Client.PatchAsJsonAsync($"/comments/{commentId}", new { body = "Not yours" });

        edit.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAttachment_OnlyUploaderOrBoardAdminCanDelete()
    {
        var (_, uploader, otherEditor, cardId) = await CreateBoardWithEditorsAsync();
        var attachmentId = await UploadPngAsync(uploader, cardId);

        (await uploader.Client.GetAsync($"/attachments/{attachmentId}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var byOtherEditor = await otherEditor.Client.DeleteAsync($"/attachments/{attachmentId}");
        byOtherEditor.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var byUploader = await uploader.Client.DeleteAsync($"/attachments/{attachmentId}");
        byUploader.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Authorization resolves the board from the attachment, so a deleted one is simply not found.
        (await uploader.Client.GetAsync($"/attachments/{attachmentId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetComments_ReturnsPagesOldestFirst()
    {
        var (admin, _, _, cardId) = await CreateBoardWithEditorsAsync();
        for (var i = 1; i <= 5; i++)
        {
            await admin.Client.PostAsJsonAsync($"/cards/{cardId}/comments", new { body = $"Comment {i}" });
        }

        var firstPage = await admin.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}/comments?pageSize=2");
        var lastPage = await admin.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}/comments?page=3&pageSize=2");

        Bodies(firstPage).ShouldBe(["Comment 1", "Comment 2"]);
        firstPage.GetProperty("totalCount").GetInt32().ShouldBe(5);
        firstPage.GetProperty("hasNextPage").GetBoolean().ShouldBeTrue();
        Bodies(lastPage).ShouldBe(["Comment 5"]);
        lastPage.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task UploadAttachment_WithContentThatDoesNotMatchTheExtension_IsRejected()
    {
        var (_, uploader, _, cardId) = await CreateBoardWithEditorsAsync();

        // A script renamed to .png: the extension is allowed, but the bytes are not a PNG.
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent("#!/bin/sh\necho not an image\n"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "screenshot.png");

        var upload = await uploader.Client.PostAsync($"/cards/{cardId}/attachments", form);

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(upload)).ShouldBe("Attachments.ContentMismatch");

        var card = await uploader.Client.GetFromJsonAsync<JsonElement>($"/cards/{cardId}");
        card.GetProperty("attachments").EnumerateArray().ShouldBeEmpty();
    }

    private static List<string?> Bodies(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("body").GetString()).ToList();

    private async Task<(TestUser Admin, TestUser Editor, TestUser OtherEditor, Guid CardId)> CreateBoardWithEditorsAsync()
    {
        var admin = await _api.CreateUserAsync();
        var editor = await _api.CreateUserAsync();
        var otherEditor = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(admin, editor, otherEditor);
        var boardId = await _api.CreateBoardAsync(admin, teamId);
        await _api.AddBoardMemberAsync(admin, boardId, editor, "Editor");
        await _api.AddBoardMemberAsync(admin, boardId, otherEditor, "Editor");
        var columnId = await _api.CreateColumnAsync(admin, boardId);
        var cardId = await _api.CreateCardAsync(admin, columnId);

        return (admin, editor, otherEditor, cardId);
    }

    private static async Task<Guid> UploadPngAsync(TestUser user, Guid cardId)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "diagram.png");

        return await ApiDriver.ReadIdAsync(await user.Client.PostAsync($"/cards/{cardId}/attachments", form));
    }
}
