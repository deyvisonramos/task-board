using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
        : this(users, refreshTokens, passwordHasher, tokenService, TimeProvider.System)
    {
    }

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var existingUser = await _users.GetByEmailAsync(email, cancellationToken);

        if (existingUser is not null)
        {
            return Result<AuthResponse>.Failure(
                "Auth.EmailAlreadyRegistered",
                "Email is already registered.");
        }

        var user = new AppUser(
            Guid.NewGuid(),
            email,
            _passwordHasher.HashPassword(request.Password),
            DateTime.UtcNow);

        if (!await _users.AddAsync(user, cancellationToken))
        {
            return Result<AuthResponse>.Failure(
                "Auth.EmailAlreadyRegistered",
                "Email is already registered.");
        }

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            return Result<AuthResponse>.Failure(
                "Auth.InvalidCredentials",
                "Email or password is invalid.");
        }

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (refreshToken is null || !refreshToken.IsActive(nowUtc))
        {
            return Result<AuthResponse>.Failure(
                "Auth.InvalidRefreshToken",
                "Refresh token is invalid or expired.");
        }

        var user = await _users.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.Failure(
                "Auth.InvalidRefreshToken",
                "Refresh token is invalid or expired.");
        }

        var result = await CreateAuthResponseAsync(user, cancellationToken);

        if (result.IsSuccess)
        {
            await _refreshTokens.RevokeAsync(
                refreshToken.Id,
                nowUtc,
                result.Value.RefreshToken is { Length: > 0 }
                    ? _tokenService.HashRefreshToken(result.Value.RefreshToken)
                    : null,
                cancellationToken);
        }

        return result;
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure(
                "Auth.UserNotFound",
                "The current user was not found.");
        }

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.CreatedAt));
    }

    private async Task<Result<AuthResponse>> CreateAuthResponseAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var tokens = _tokenService.CreateTokens(user);
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            tokens.RefreshTokenHash,
            tokens.RefreshTokenExpiresAt,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            new UserDto(user.Id, user.Email, user.CreatedAt),
            tokens.AccessToken,
            tokens.RefreshToken));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
