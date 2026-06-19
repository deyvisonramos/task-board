using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<AppUser> Items { get; } = [];

    public Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        if (Items.Any(existingUser => existingUser.Email == user.Email))
        {
            return Task.FromResult(false);
        }

        Items.Add(user);
        return Task.FromResult(true);
    }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(user => user.Email == email));
    }

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(user => user.Id == id));
    }
}
