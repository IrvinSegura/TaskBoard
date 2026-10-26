using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Authorization;

namespace TaskBoard.Infrastructure.IntegrationTests.Authorization;

public class BoardAccessServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task GetRoleAsync_ReturnsRoleForMembersAndNullForStrangers()
    {
        var owner = User.Create($"{Guid.NewGuid():N}@test.local", "Owner", "hash");
        var stranger = User.Create($"{Guid.NewGuid():N}@test.local", "Stranger", "hash");
        var team = Team.Create("Team", owner.Id);
        var board = Board.Create(team.Id, "Private board", owner.Id, BoardVisibility.Private);

        await using (var db = fixture.CreateDbContext())
        {
            db.Users.AddRange(owner, stranger);
            db.Teams.Add(team);
            db.Boards.Add(board);
            await db.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var service = new BoardAccessService(context);

        (await service.GetRoleAsync(board.Id, owner.Id, CancellationToken.None)).ShouldBe(BoardRole.Admin);
        (await service.GetRoleAsync(board.Id, stranger.Id, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task GetRoleAsync_TeamVisibleBoard_GivesTeamMembersViewerRole()
    {
        var owner = User.Create($"{Guid.NewGuid():N}@test.local", "Owner", "hash");
        var teammate = User.Create($"{Guid.NewGuid():N}@test.local", "Teammate", "hash");
        var team = Team.Create("Team", owner.Id);
        team.AddMember(owner.Id, teammate.Id, TeamRole.Member);
        var board = Board.Create(team.Id, "Team board", owner.Id, BoardVisibility.Team);

        await using (var db = fixture.CreateDbContext())
        {
            db.Users.AddRange(owner, teammate);
            db.Teams.Add(team);
            db.Boards.Add(board);
            await db.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var service = new BoardAccessService(context);

        (await service.GetRoleAsync(board.Id, teammate.Id, CancellationToken.None)).ShouldBe(BoardRole.Viewer);
    }
}
