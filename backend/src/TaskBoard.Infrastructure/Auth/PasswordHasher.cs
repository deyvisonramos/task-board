using Microsoft.AspNetCore.Identity;
using TaskBoard.Application.Auth;

namespace TaskBoard.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(User, password);
    }

    public bool VerifyPassword(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(User, passwordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
