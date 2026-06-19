using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = TryGetUserId(out var userId)
            ? await _authService.GetCurrentUserAsync(userId, cancellationToken)
            : Result<UserDto>.Failure("Auth.InvalidToken", "The access token is invalid.");

        return FromResult(result, Ok);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out userId);
    }
}
