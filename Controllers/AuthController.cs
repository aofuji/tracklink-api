using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrackLink.DTOs;
using TrackLink.Services;

namespace TrackLink.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "tracklink_refresh_token";
    private const string RefreshTokenCookiePath = "/api/auth";

    private readonly AuthService _authService;
    private readonly TokenService _tokenService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        AuthService authService,
        TokenService tokenService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _tokenService = tokenService;
        _environment = environment;
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

        AppendRefreshTokenCookie(refreshToken);

        return Ok(new
        {
            accessToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenFromCookie();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var user = await _authService.RotateRefreshTokenAsync(
            refreshToken,
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

        AppendRefreshTokenCookie(newRefreshToken);

        return Ok(new
        {
            accessToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenFromCookie();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new
            {
                message = "Invalid refresh token."
            });
        }

        var revoked = await _authService.RevokeRefreshTokenAsync(
            refreshToken,
            cancellationToken
        );

        if (!revoked)
        {
            return Unauthorized(new
            {
                message = "Invalid refresh token."
            });
        }

        DeleteRefreshTokenCookie();

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new
        {
            id = userId,
            name,
            email
        });
    }

    private string? GetRefreshTokenFromCookie()
    {
        return Request.Cookies[RefreshTokenCookieName];
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            CreateRefreshTokenCookieOptions(includeExpiration: true)
        );
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshTokenCookieOptions(includeExpiration: false)
        );
    }

    private CookieOptions CreateRefreshTokenCookieOptions(bool includeExpiration)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = _environment.IsProduction(),
            SameSite = SameSiteMode.Lax,
            Path = RefreshTokenCookiePath
        };

        if (includeExpiration)
        {
            options.Expires = DateTimeOffset.UtcNow.Add(AuthService.RefreshTokenLifetime);
        }

        return options;
    }
}
