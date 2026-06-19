using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
}
