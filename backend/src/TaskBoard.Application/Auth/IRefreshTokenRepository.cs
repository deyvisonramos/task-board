using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid id,
        DateTime revokedAt,
        string? replacedByTokenHash,
        CancellationToken cancellationToken = default);
}
