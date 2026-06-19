using FluentAssertions;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

public sealed class AuthServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _refreshTokens, _passwordHasher, _tokenService);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_Succeeds()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("new@example.com", "Password123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("new@example.com");
        result.Value.User.GetType().GetProperty("PasswordHash").Should().BeNull();
        result.Value.AccessToken.Should().Be("access-token-1");
        result.Value.RefreshToken.Should().Be("refresh-token-1");
        _users.Items.Should().ContainSingle(user =>
            user.Email == "new@example.com" &&
            user.PasswordHash == "hashed:Password123!");
        _refreshTokens.Items.Should().ContainSingle(token =>
            token.UserId == result.Value.User.Id &&
            token.TokenHash == "hash:refresh-token-1");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_Fails()
    {
        _users.Items.Add(NewUser("duplicate@example.com", "existing-hash"));

        var result = await _service.RegisterAsync(new RegisterRequest("duplicate@example.com", "Password123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Auth.EmailAlreadyRegistered");
        _users.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_Succeeds()
    {
        _users.Items.Add(NewUser("demo@example.com", "hashed:Demo123!"));

        var result = await _service.LoginAsync(new LoginRequest("demo@example.com", "Demo123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("demo@example.com");
        result.Value.AccessToken.Should().Be("access-token-1");
        result.Value.RefreshToken.Should().Be("refresh-token-1");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_Fails()
    {
        _users.Items.Add(NewUser("demo@example.com", "hashed:Demo123!"));

        var result = await _service.LoginAsync(new LoginRequest("demo@example.com", "WrongPassword123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task RefreshAsync_WithActiveRefreshToken_RotatesToken()
    {
        var user = NewUser("demo@example.com", "hashed:Demo123!");
        _users.Items.Add(user);
        _refreshTokens.Items.Add(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            "hash:old-refresh-token",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow));

        var result = await _service.RefreshAsync(new RefreshTokenRequest("old-refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-1");
        result.Value.RefreshToken.Should().Be("refresh-token-1");
        _refreshTokens.Items.Should().Contain(token =>
            token.TokenHash == "hash:old-refresh-token" &&
            token.RevokedAt != null &&
            token.ReplacedByTokenHash == "hash:refresh-token-1");
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownRefreshToken_Fails()
    {
        var result = await _service.RefreshAsync(new RefreshTokenRequest("missing-refresh-token"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    private static AppUser NewUser(string email, string passwordHash)
    {
        return new AppUser(Guid.NewGuid(), email, passwordHash, DateTime.UtcNow);
    }
}
