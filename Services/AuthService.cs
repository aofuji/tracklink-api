using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrackLink.Data;
using TrackLink.Models;
using System.Security.Cryptography;
using System.Text;

namespace TrackLink.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<User?> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var emailExists = await _context.Users
            .AnyAsync(
                x => x.Email == email,
                cancellationToken
            );

        if (emailExists)
        {
            return null;
        }

        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password
        );

        _context.Users.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User?> LoginAsync(
    string email,
    string password,
    CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == email,
                cancellationToken
            );

        if (user == null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return user;
    }

    public async Task SaveRefreshTokenAsync(
    User user,
    string refreshToken,
    CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var token = new RefreshToken
        {
            TokenHash = tokenHash,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(token);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> RotateRefreshTokenAsync(
    string refreshToken,
    string newRefreshToken,
    CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken
            );

        if (storedToken == null)
        {
            return null;
        }

        if (storedToken.IsRevoked)
        {
            return null;
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        // O token antigo não poderá mais ser utilizado.
        storedToken.IsRevoked = true;

        var newTokenHash = HashRefreshToken(newRefreshToken);

        var newToken = new RefreshToken
        {
            TokenHash = newTokenHash,
            UserId = storedToken.UserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newToken);

        await _context.SaveChangesAsync(cancellationToken);

        return storedToken.User;
    }

    public async Task<bool> RevokeRefreshTokenAsync(
    string refreshToken,
    CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken
            );

        if (storedToken == null)
        {
            return false;
        }

        if (storedToken.IsRevoked)
        {
            return false;
        }

        storedToken.IsRevoked = true;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)
            )
        );
    }
}
