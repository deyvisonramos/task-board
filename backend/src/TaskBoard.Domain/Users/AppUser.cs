namespace TaskBoard.Domain.Users;

public sealed class AppUser
{
    public AppUser(Guid id, string email, string passwordHash, DateTime createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("User creation timestamp is required.", nameof(createdAt));
        }

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
