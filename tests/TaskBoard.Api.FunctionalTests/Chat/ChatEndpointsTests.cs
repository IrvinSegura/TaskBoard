using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskBoard.Api.FunctionalTests.Chat;

public class ChatEndpointsTests(TaskBoardApiFactory factory) : IClassFixture<TaskBoardApiFactory>
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    // "....ftypM4A ": what a voice note recorded in Safari starts with.
    private static readonly byte[] M4aBytes = [0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41, 0x20];

    private readonly ApiDriver _api = new(factory);

    [Fact]
    public async Task DirectConversation_IsTheSameWhoeverStartsItAndByIdOrEmail()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();

        var byEmail = await StartDirectAsync(ana, new { email = beto.Email });
        var again = await StartDirectAsync(ana, new { userId = beto.Id });
        var fromBeto = await StartDirectAsync(beto, new { userId = ana.Id });

        again.ShouldBe(byEmail);
        fromBeto.ShouldBe(byEmail);
    }

    [Fact]
    public async Task DirectConversation_WithYourselfOrNobody_Fails()
    {
        var ana = await _api.CreateUserAsync();

        var self = await ana.Client.PostAsJsonAsync("/chat/conversations/direct", new { userId = ana.Id });
        self.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(self)).ShouldBe("Chat.CannotMessageYourself");

        var nobody = await ana.Client.PostAsJsonAsync("/chat/conversations/direct", new { email = "nadie@example.com" });
        nobody.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Messages_CountAsUnreadUntilTheConversationIsRead()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });

        var sent = await SendAsync(ana, conversationId, "Hola, ¿revisas https://example.com/plan?");
        sent.GetProperty("body").GetString().ShouldBe("Hola, ¿revisas https://example.com/plan?");
        sent.GetProperty("senderId").GetGuid().ShouldBe(ana.Id);

        var inbox = await GetConversationAsync(beto, conversationId);
        inbox.GetProperty("kind").GetString().ShouldBe("Direct");
        inbox.GetProperty("unreadCount").GetInt32().ShouldBe(1);
        inbox.GetProperty("lastMessage").GetProperty("senderId").GetGuid().ShouldBe(ana.Id);
        // Your own messages never count as unread.
        (await GetConversationAsync(ana, conversationId)).GetProperty("unreadCount").GetInt32().ShouldBe(0);

        (await beto.Client.PostAsync($"/chat/conversations/{conversationId}/read", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await GetConversationAsync(beto, conversationId)).GetProperty("unreadCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Outsiders_CannotSeeReadOrWriteAConversation()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var outsider = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });
        var sent = await SendAsync(ana, conversationId, "Privado", ("foto.png", PngBytes));
        var attachmentId = sent.GetProperty("attachments")[0].GetProperty("id").GetGuid();

        (await outsider.Client.GetAsync($"/chat/conversations/{conversationId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Client.GetAsync($"/chat/conversations/{conversationId}/messages")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Client.GetAsync($"/chat/attachments/{attachmentId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostMessageAsync(outsider, conversationId, "Hola")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var list = await outsider.Client.GetFromJsonAsync<JsonElement>("/chat/conversations");
        list.EnumerateArray().ShouldNotContain(c => c.GetProperty("id").GetGuid() == conversationId);
    }

    [Fact]
    public async Task TeamsAndBoards_HaveTheirOwnConversation_ForTheirMembersOnly()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var teamId = await _api.CreateTeamAsync(owner, member);
        var privateBoardId = await _api.CreateBoardAsync(owner, teamId);

        var teamChat = await FindConversationAsync(member, c => c.GetProperty("teamId").GetGuid() == teamId &&
            c.GetProperty("kind").GetString() == "Team");
        teamChat.ShouldNotBeNull();
        await SendAsync(member, teamChat.Value.GetProperty("id").GetGuid(), "Hola equipo");

        // A private board's chat is only for its members, even inside the team.
        (await FindConversationAsync(member, IsBoard(privateBoardId))).ShouldBeNull();
        var boardChat = await FindConversationAsync(owner, IsBoard(privateBoardId));
        boardChat.ShouldNotBeNull();
        boardChat.Value.GetProperty("title").GetString().ShouldBe("Board");

        await _api.AddBoardMemberAsync(owner, privateBoardId, member, "Viewer");
        (await FindConversationAsync(member, IsBoard(privateBoardId))).ShouldNotBeNull();

        // Team-visible boards are open to the whole team.
        var teamBoardId = await _api.CreateBoardAsync(owner, teamId, visibility: "Team");
        (await FindConversationAsync(member, IsBoard(teamBoardId))).ShouldNotBeNull();
    }

    [Fact]
    public async Task Files_AreStoredWithTheirTypeAndDownloadedByMembers()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });

        var sent = await SendAsync(ana, conversationId, null, ("foto.png", PngBytes), ("nota-de-voz.m4a", M4aBytes));

        sent.GetProperty("body").ValueKind.ShouldBe(JsonValueKind.Null);
        var attachments = sent.GetProperty("attachments").EnumerateArray().ToList();
        attachments.Select(a => a.GetProperty("contentType").GetString()).ShouldBe(["image/png", "audio/mp4"]);

        var download = await beto.Client.GetAsync($"/chat/attachments/{attachments[0].GetProperty("id").GetGuid()}");
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).ShouldBe(PngBytes);
        download.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
    }

    [Fact]
    public async Task Files_ThatLieAboutTheirTypeOrAreNotAllowed_AreRejected()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });

        var renamed = await PostMessageAsync(ana, conversationId, null, ("foto.png", "#!/bin/sh\n"u8.ToArray()));
        renamed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiDriver.ReadErrorCodeAsync(renamed)).ShouldBe("Chat.ContentMismatch");

        var executable = await PostMessageAsync(ana, conversationId, null, ("setup.exe", [0x4D, 0x5A]));
        executable.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var empty = await PostMessageAsync(ana, conversationId, "   ");
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var page = await ana.Client.GetFromJsonAsync<JsonElement>($"/chat/conversations/{conversationId}/messages");
        page.GetProperty("items").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task OnlyTheSender_CanEditOrDeleteAMessage()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });
        var sent = await SendAsync(ana, conversationId, "Borrador", ("foto.png", PngBytes));
        var messageId = sent.GetProperty("id").GetGuid();
        var attachmentId = sent.GetProperty("attachments")[0].GetProperty("id").GetGuid();

        (await beto.Client.PatchAsJsonAsync($"/chat/messages/{messageId}", new { body = "Hackeado" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await beto.Client.DeleteAsync($"/chat/messages/{messageId}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await ana.Client.PatchAsJsonAsync($"/chat/messages/{messageId}", new { body = "Final" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var edited = await GetMessageAsync(beto, conversationId, messageId);
        edited.GetProperty("body").GetString().ShouldBe("Final");
        edited.GetProperty("isEdited").GetBoolean().ShouldBeTrue();

        (await ana.Client.DeleteAsync($"/chat/messages/{messageId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var deleted = await GetMessageAsync(beto, conversationId, messageId);
        deleted.GetProperty("isDeleted").GetBoolean().ShouldBeTrue();
        deleted.GetProperty("body").ValueKind.ShouldBe(JsonValueKind.Null);
        deleted.GetProperty("attachments").EnumerateArray().ShouldBeEmpty();
        (await beto.Client.GetAsync($"/chat/attachments/{attachmentId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Messages_ArePagedNewestFirstAndShownOldestFirst()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var conversationId = await StartDirectAsync(ana, new { userId = beto.Id });
        for (var i = 1; i <= 5; i++)
        {
            await SendAsync(ana, conversationId, $"Mensaje {i}");
        }

        var newest = await beto.Client.GetFromJsonAsync<JsonElement>($"/chat/conversations/{conversationId}/messages?limit=2");
        Bodies(newest).ShouldBe(["Mensaje 4", "Mensaje 5"]);
        newest.GetProperty("hasMore").GetBoolean().ShouldBeTrue();

        var before = newest.GetProperty("items")[0].GetProperty("id").GetGuid();
        var older = await beto.Client.GetFromJsonAsync<JsonElement>(
            $"/chat/conversations/{conversationId}/messages?limit=3&before={before}");
        Bodies(older).ShouldBe(["Mensaje 1", "Mensaje 2", "Mensaje 3"]);
        older.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Groups_CanBeRenamedJoinedAndLeft()
    {
        var ana = await _api.CreateUserAsync();
        var beto = await _api.CreateUserAsync();
        var carla = await _api.CreateUserAsync();
        var diego = await _api.CreateUserAsync();

        var create = await ana.Client.PostAsJsonAsync(
            "/chat/conversations/group", new { title = "Lanzamiento", participantIds = new[] { beto.Id, carla.Id } });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var groupId = await ApiDriver.ReadIdAsync(create);

        (await beto.Client.PatchAsJsonAsync($"/chat/conversations/{groupId}", new { title = "Lanzamiento v2" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await beto.Client.PostAsJsonAsync($"/chat/conversations/{groupId}/participants", new { userIds = new[] { diego.Id } }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await carla.Client.DeleteAsync($"/chat/conversations/{groupId}/participants/me"))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var group = await GetConversationAsync(diego, groupId);
        group.GetProperty("title").GetString().ShouldBe("Lanzamiento v2");
        group.GetProperty("participants").EnumerateArray().Select(p => p.GetProperty("id").GetGuid())
            .ShouldBe([ana.Id, beto.Id, diego.Id], ignoreOrder: true);

        (await carla.Client.GetAsync($"/chat/conversations/{groupId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Contacts_ArePeopleYouShareATeamOrBoardWith()
    {
        var owner = await _api.CreateUserAsync();
        var member = await _api.CreateUserAsync();
        var stranger = await _api.CreateUserAsync();
        await _api.CreateTeamAsync(owner, member);

        var contacts = await owner.Client.GetFromJsonAsync<JsonElement>("/chat/contacts");

        var ids = contacts.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
        ids.ShouldBe([member.Id]);
        ids.ShouldNotContain(stranger.Id);
    }

    private static Func<JsonElement, bool> IsBoard(Guid boardId) =>
        c => c.GetProperty("boardId").ValueKind != JsonValueKind.Null && c.GetProperty("boardId").GetGuid() == boardId;

    private static IEnumerable<string?> Bodies(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(m => m.GetProperty("body").GetString());

    private static async Task<Guid> StartDirectAsync(TestUser user, object request) =>
        await ApiDriver.ReadIdAsync(await user.Client.PostAsJsonAsync("/chat/conversations/direct", request));

    private static async Task<JsonElement> GetConversationAsync(TestUser user, Guid conversationId) =>
        await user.Client.GetFromJsonAsync<JsonElement>($"/chat/conversations/{conversationId}");

    private static async Task<JsonElement?> FindConversationAsync(TestUser user, Func<JsonElement, bool> match)
    {
        var list = await user.Client.GetFromJsonAsync<JsonElement>("/chat/conversations");
        return list.EnumerateArray().Where(match).Cast<JsonElement?>().FirstOrDefault();
    }

    private static async Task<JsonElement> GetMessageAsync(TestUser user, Guid conversationId, Guid messageId)
    {
        var page = await user.Client.GetFromJsonAsync<JsonElement>($"/chat/conversations/{conversationId}/messages");
        return page.GetProperty("items").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == messageId);
    }

    private static async Task<JsonElement> SendAsync(
        TestUser user,
        Guid conversationId,
        string? body,
        params (string Name, byte[] Content)[] files)
    {
        var response = await PostMessageAsync(user, conversationId, body, files);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<HttpResponseMessage> PostMessageAsync(
        TestUser user,
        Guid conversationId,
        string? body,
        params (string Name, byte[] Content)[] files)
    {
        using var form = new MultipartFormDataContent();
        if (body is not null)
        {
            form.Add(new StringContent(body), "body");
        }

        foreach (var (name, content) in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "files", name);
        }

        return await user.Client.PostAsync($"/chat/conversations/{conversationId}/messages", form);
    }
}
