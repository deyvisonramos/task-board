namespace TaskBoard.Application.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record UserDto(Guid Id, string Email, DateTime CreatedAt);

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    string RefreshTokenHash,
    DateTime RefreshTokenExpiresAt);

public sealed record AuthResponse(UserDto User, string AccessToken, string RefreshToken);
