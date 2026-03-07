using Api.Authentication;
using Application.Auth.Contracts;
using Application.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        return result.Status switch
        {
            RegisterAuthStatus.Created => StatusCode(StatusCodes.Status201Created, result.User),
            RegisterAuthStatus.Conflict => Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = result.ErrorDetail
            }),
            RegisterAuthStatus.BadRequest => BadRequest(new ValidationProblemDetails(result.Errors ?? new Dictionary<string, string[]>())
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest
            }),
            _ => throw new InvalidOperationException("Unsupported register result status.")
        };
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        return result.Status switch
        {
            LoginAuthStatus.Success => Ok(result.Tokens),
            LoginAuthStatus.Unauthorized => Unauthorized(CreateUnauthorizedProblemDetails()),
            _ => throw new InvalidOperationException("Unsupported login result status.")
        };
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);

        return result.Status switch
        {
            RefreshAuthStatus.Success => Ok(result.Tokens),
            RefreshAuthStatus.Unauthorized => Unauthorized(CreateUnauthorizedProblemDetails()),
            _ => throw new InvalidOperationException("Unsupported refresh result status.")
        };
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorizedProblemDetails());
        }

        var result = await _authService.LogoutAsync(userId.Value, request, cancellationToken);

        return result.Status switch
        {
            LogoutAuthStatus.NoContent => NoContent(),
            LogoutAuthStatus.Unauthorized => Unauthorized(CreateUnauthorizedProblemDetails()),
            _ => throw new InvalidOperationException("Unsupported logout result status.")
        };
    }

    private static ProblemDetails CreateUnauthorizedProblemDetails()
    {
        return new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        };
    }
}
