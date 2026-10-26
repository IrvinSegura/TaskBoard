using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Comments;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class CommentRepository(ApplicationDbContext db) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken) =>
        await db.Comments.FindAsync([commentId], cancellationToken);

    public void Add(Comment comment) => db.Comments.Add(comment);

    public void Remove(Comment comment) => db.Comments.Remove(comment);
}
