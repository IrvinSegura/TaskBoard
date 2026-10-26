using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Authorization;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Interceptors;

namespace TaskBoard.Infrastructure.IntegrationTests.Authorization;

public class CachedBoardAccessServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task GetRoleAsync_ServesTheCachedRoleUntilAMembershipChangeEvictsIt()
    {
        var owner = User.Create($"{Guid.NewGuid():N}@test.local", "Owner", "hash");
        var member = User.Create($"{Guid.NewGuid():N}@test.local", "Member", "hash");
        var team = Team.Create("Team", owner.Id);
        var board = Board.Create(team.Id, "Board", owner.Id, BoardVisibility.Private);
        board.AddMember(member.Id, BoardRole.Editor);
        await using (var db = fixture.CreateDbContext())
        {
            db.Users.AddRange(owner, member);
            db.Teams.Add(team);
            db.Boards.Add(board);
            await db.SaveChangesAsync();
        }

        (await GetRoleAsync(board.Id, member.Id)).ShouldBe(BoardRole.Editor);

        // Changed behind the cache's back (no interceptor), so the cached role is still served.
        await ChangeBoardAsync(board.Id, b => b.RemoveMember(member.Id), withInterceptor: false);
        (await GetRoleAsync(board.Id, member.Id)).ShouldBe(BoardRole.Editor);

        // Changed through the application's DbContext setup, so the entry is evicted.
        await ChangeBoardAsync(board.Id, b => b.AddMember(member.Id, BoardRole.Viewer), withInterceptor: true);
        (await GetRoleAsync(board.Id, member.Id)).ShouldBe(BoardRole.Viewer);
    }

    [Fact]
    public async Task GetRoleAsync_JoiningATeam_EvictsTheCachedNoAccessForItsTeamBoards()
    {
        var owner = User.Create($"{Guid.NewGuid():N}@test.local", "Owner", "hash");
        var newcomer = User.Create($"{Guid.NewGuid():N}@test.local", "Newcomer", "hash");
        var team = Team.Create("Team", owner.Id);
        var board = Board.Create(team.Id, "Team board", owner.Id, BoardVisibility.Team);
        await using (var db = fixture.CreateDbContext())
        {
            db.Users.AddRange(owner, newcomer);
            db.Teams.Add(team);
            db.Boards.Add(board);
            await db.SaveChangesAsync();
        }

        (await GetRoleAsync(board.Id, newcomer.Id)).ShouldBeNull();

        await using (var db = fixture.CreateDbContext(CreateInterceptor()))
        {
            var trackedTeam = await db.Teams.Include(t => t.Members).SingleAsync(t => t.Id == team.Id);
            trackedTeam.AddMember(owner.Id, newcomer.Id, TeamRole.Member).IsSuccess.ShouldBeTrue();
            await db.SaveChangesAsync();
        }

        (await GetRoleAsync(board.Id, newcomer.Id)).ShouldBe(BoardRole.Viewer);
    }

    private async Task<BoardRole?> GetRoleAsync(Guid boardId, Guid userId)
    {
        await using var db = fixture.CreateDbContext();
        var service = new CachedBoardAccessService(
            new BoardAccessService(db), _cache, NullLogger<CachedBoardAccessService>.Instance);

        return await service.GetRoleAsync(boardId, userId, CancellationToken.None);
    }

    private async Task ChangeBoardAsync(Guid boardId, Action<Board> change, bool withInterceptor)
    {
        await using var db = withInterceptor ? fixture.CreateDbContext(CreateInterceptor()) : fixture.CreateDbContext();
        var board = await db.Boards.Include(b => b.Members).SingleAsync(b => b.Id == boardId);
        change(board);
        await db.SaveChangesAsync();
    }

    private BoardRoleCacheInvalidationInterceptor CreateInterceptor() =>
        new(_cache, NullLogger<BoardRoleCacheInvalidationInterceptor>.Instance);
}
