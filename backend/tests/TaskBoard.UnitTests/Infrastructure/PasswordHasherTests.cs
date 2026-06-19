using FluentAssertions;
using TaskBoard.Infrastructure.Auth;

namespace TaskBoard.UnitTests.Infrastructure;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashPassword_DoesNotReturnPlaintextPassword()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.HashPassword("Demo123!");

        hash.Should().NotBe("Demo123!");
        hash.Should().NotContain("Demo123!");
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueOnlyForMatchingPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("Demo123!");

        hasher.VerifyPassword(hash, "Demo123!").Should().BeTrue();
        hasher.VerifyPassword(hash, "WrongPassword123!").Should().BeFalse();
    }
}
