using FluentAssertions;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

public sealed class AuthServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _passwordHasher, _tokenService);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_Succeeds()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("new@example.com", "Password123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("new@example.com");
        result.Value.User.GetType().GetProperty("PasswordHash").Should().BeNull();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        _users.Items.Should().ContainSingle(user =>
            user.Email == "new@example.com" &&
            user.PasswordHash == "hashed:Password123!");
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
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_Fails()
    {
        _users.Items.Add(NewUser("demo@example.com", "hashed:Demo123!"));

        var result = await _service.LoginAsync(new LoginRequest("demo@example.com", "WrongPassword123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    private static AppUser NewUser(string email, string passwordHash)
    {
        return new AppUser(Guid.NewGuid(), email, passwordHash, DateTime.UtcNow);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<AppUser> Items { get; } = [];

        public Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            Items.Add(user);
            return Task.CompletedTask;
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return $"hashed:{password}";
        }

        public bool VerifyPassword(string passwordHash, string password)
        {
            return passwordHash == $"hashed:{password}";
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public TokenPair CreateTokens(AppUser user)
        {
            return new TokenPair("access-token", "refresh-token");
        }
    }
}
