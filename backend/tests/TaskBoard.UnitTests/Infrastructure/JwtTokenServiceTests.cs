using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Auth;

namespace TaskBoard.UnitTests.Infrastructure;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateTokens_IncludesUserClaimsAndConfiguredExpiration()
    {
        var user = new AppUser(
            Guid.NewGuid(),
            "demo@example.com",
            "hashed-password",
            DateTime.UtcNow);

        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "TaskBoard",
            Audience = "TaskBoard.Api",
            SigningKey = "test-signing-key-with-at-least-32-bytes",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 14
        }));

        var tokens = service.CreateTokens(user);
        var payload = ReadPayload(tokens.AccessToken);

        payload.GetProperty("sub").GetString().Should().Be(user.Id.ToString());
        payload.GetProperty("email").GetString().Should().Be(user.Email);
        payload.GetProperty("iss").GetString().Should().Be("TaskBoard");
        payload.GetProperty("aud").GetString().Should().Be("TaskBoard.Api");
        var issuedAt = payload.GetProperty("iat").GetInt64();
        var expiresAt = payload.GetProperty("exp").GetInt64();
        (expiresAt - issuedAt).Should().Be(1_800);
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBe(tokens.AccessToken);
    }

    [Fact]
    public void CreateTokens_WithMissingSigningKey_FailsFast()
    {
        var createService = () => new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "TaskBoard",
            Audience = "TaskBoard.Api",
            SigningKey = "too-short",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 14
        }));

        createService.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT signing key must be at least 32 bytes.");
    }

    private static JsonElement ReadPayload(string accessToken)
    {
        var parts = accessToken.Split('.');
        parts.Should().HaveCount(3);

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = payload.Length % 4;

        if (padding > 0)
        {
            payload = payload.PadRight(payload.Length + 4 - padding, '=');
        }

        return JsonDocument.Parse(Convert.FromBase64String(payload)).RootElement.Clone();
    }
}
