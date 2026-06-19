using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

internal sealed class FakeTokenService : ITokenService
{
    private int _tokenNumber;

    public TokenPair CreateTokens(AppUser user)
    {
        _tokenNumber++;
        var refreshToken = $"refresh-token-{_tokenNumber}";

        return new TokenPair(
            $"access-token-{_tokenNumber}",
            refreshToken,
            HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddDays(30));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return $"hash:{refreshToken}";
    }
}
