using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);

    void Add(User user);
}
