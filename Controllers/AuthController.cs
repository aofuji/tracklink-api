using Microsoft.AspNetCore.Mvc;
using TrackLink.DTOs;
using TrackLink.Services;

namespace TrackLink.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly TokenService _tokenService;

    public AuthController(
    AuthService authService,
    TokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _authService.RegisterAsync(
            request.Name,
            request.Email,
            request.Password,
            cancellationToken
        );

        if (user == null)
        {
            return Conflict(new
            {
                message = "Email is already registered."
            });
        }

        return Created("", new
        {
            user.Id,
            user.Name,
            user.Email,
            user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
      LoginRequest request,
      CancellationToken cancellationToken)
    {
        var user = await _authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken
        );

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        var refreshToken = _tokenService.GenerateRefreshToken();

        await _authService.SaveRefreshTokenAsync(
            user,
            refreshToken,
            cancellationToken
        );

        return Ok(new
        {
            accessToken,
            refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(
       RefreshTokenRequest request,
       CancellationToken cancellationToken)
    {
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var user = await _authService.RotateRefreshTokenAsync(
            request.RefreshToken,
            newRefreshToken,
            cancellationToken
        );

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        return Ok(new
        {
            accessToken,
            refreshToken = newRefreshToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
    RefreshTokenRequest request,
    CancellationToken cancellationToken)
    {
        var revoked = await _authService.RevokeRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken
        );

        if (!revoked)
        {
            return Unauthorized(new
            {
                message = "Invalid refresh token."
            });
        }

        return NoContent();
    }
}