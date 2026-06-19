namespace TaskBoard.Domain.Users;

public sealed class AppUser
{
    public AppUser(Guid id, string email, string passwordHash, DateTime createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string PasswordHash { get; }

    public DateTime CreatedAt { get; }
}
