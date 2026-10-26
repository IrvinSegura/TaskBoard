using TaskBoard.Domain.Comments;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken);

    void Add(Comment comment);

    void Remove(Comment comment);
}
