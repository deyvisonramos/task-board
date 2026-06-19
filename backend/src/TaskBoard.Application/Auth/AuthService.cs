using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCredentials(request.Email, request.Password);

        if (validationErrors.Count > 0)
        {
            return Result<AuthResponse>.ValidationFailure(validationErrors);
        }

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

        await _users.AddAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
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

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
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

    private static List<ValidationError> ValidateCredentials(string email, string password)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ValidationError("Auth.EmailRequired", "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(new ValidationError("Auth.PasswordRequired", "Password is required."));
        }

        return errors;
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var tokens = _tokenService.CreateTokens(user);

        return new AuthResponse(
            new UserDto(user.Id, user.Email, user.CreatedAt),
            tokens.AccessToken,
            tokens.RefreshToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
