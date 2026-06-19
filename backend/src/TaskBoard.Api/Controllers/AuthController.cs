using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Auth;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "User registered successfully. UserId: {UserId}",
                result.Value.User.Id);
        }

        return FromResult(result, Ok);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Login succeeded. UserId: {UserId}",
                result.Value.User.Id);
        }
        else
        {
            _logger.LogInformation(
                "Login failed. ErrorCode: {ErrorCode}",
                result.Error?.Code ?? "Unknown.Error");
        }

        return FromResult(result, Ok);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Token refresh succeeded. UserId: {UserId}",
                result.Value.User.Id);
        }
        else
        {
            _logger.LogInformation(
                "Token refresh failed. ErrorCode: {ErrorCode}",
                result.Error?.Code ?? "Unknown.Error");
        }

        return FromResult(result, Ok);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);

        return FromResult(result, Ok);
    }
}
