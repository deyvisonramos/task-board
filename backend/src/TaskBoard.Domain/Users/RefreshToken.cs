namespace TaskBoard.Domain.Users;

public sealed class RefreshToken
{
    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime? revokedAt = null,
        string? replacedByTokenHash = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Refresh token id is required.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Refresh token hash is required.", nameof(tokenHash));
        }

        if (expiresAt == default)
        {
            throw new ArgumentException("Refresh token expiration is required.", nameof(expiresAt));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Refresh token creation timestamp is required.", nameof(createdAt));
        }

        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public string TokenHash { get; }

    public DateTime ExpiresAt { get; }

    public DateTime CreatedAt { get; }

    public DateTime? RevokedAt { get; }

    public string? ReplacedByTokenHash { get; }

    public bool IsActive(DateTime nowUtc)
    {
        return RevokedAt is null && ExpiresAt > nowUtc;
    }
}
