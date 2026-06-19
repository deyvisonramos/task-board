using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Items.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(token => token.TokenHash == tokenHash));
    }

    public Task RevokeAsync(
        Guid id,
        DateTime revokedAt,
        string? replacedByTokenHash,
        CancellationToken cancellationToken = default)
    {
        var index = Items.FindIndex(token => token.Id == id);

        if (index >= 0)
        {
            var token = Items[index];
            Items[index] = new RefreshToken(
                token.Id,
                token.UserId,
                token.TokenHash,
                token.ExpiresAt,
                token.CreatedAt,
                revokedAt,
                replacedByTokenHash);
        }

        return Task.CompletedTask;
    }
}
