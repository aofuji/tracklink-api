using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrackLink.Models;
using System.Security.Cryptography;

namespace TrackLink.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expirationMinutes = int.Parse(
            _configuration["Jwt:ExpirationMinutes"] ?? "15"
        );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
}