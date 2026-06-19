using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private const string Algorithm = "HS256";
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> options)
        : this(options, TimeProvider.System)
    {
    }

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        ValidateOptions(_options);
    }

    public TokenPair CreateTokens(AppUser user)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenExpirationMinutes);

        var header = new Dictionary<string, object>
        {
            ["alg"] = Algorithm,
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["jti"] = Guid.NewGuid().ToString(),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds()
        };

        return new TokenPair(CreateJwt(header, payload), CreateRefreshToken());
    }

    private string CreateJwt(
        IReadOnlyDictionary<string, object> header,
        IReadOnlyDictionary<string, object> payload)
    {
        var unsignedToken = string.Join(
            ".",
            Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header)),
            Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload)));

        var signature = Sign(unsignedToken);

        return $"{unsignedToken}.{signature}";
    }

    private string Sign(string unsignedToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
    }

    private static string CreateRefreshToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT audience is required.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        if (options.AccessTokenExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT access token expiration must be greater than zero.");
        }

        if (options.RefreshTokenExpirationDays <= 0)
        {
            throw new InvalidOperationException("JWT refresh token expiration must be greater than zero.");
        }
    }
}
