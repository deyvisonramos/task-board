using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public interface ITokenService
{
    TokenPair CreateTokens(AppUser user);
}
