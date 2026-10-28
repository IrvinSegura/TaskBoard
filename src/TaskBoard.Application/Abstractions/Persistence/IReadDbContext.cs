using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Favorites;
using TaskBoard.Domain.Labels;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Read side for queries: untracked sets projected straight into response DTOs.
/// Commands go through the repositories instead.
/// </summary>
public interface IReadDbContext
{
    IQueryable<User> Users { get; }

    IQueryable<Team> Teams { get; }

    IQueryable<TeamMember> TeamMembers { get; }

    IQueryable<Board> Boards { get; }

    IQueryable<BoardMember> BoardMembers { get; }

    IQueryable<Column> Columns { get; }

    IQueryable<Card> Cards { get; }

    IQueryable<Comment> Comments { get; }

    IQueryable<Attachment> Attachments { get; }

    IQueryable<Notification> Notifications { get; }

    IQueryable<Label> Labels { get; }

    IQueryable<ChecklistItem> ChecklistItems { get; }

    IQueryable<ActivityEntry> Activities { get; }

    IQueryable<Favorite> Favorites { get; }
}
