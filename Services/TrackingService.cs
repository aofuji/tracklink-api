using Microsoft.EntityFrameworkCore;
using TrackLink.Data;
using TrackLink.Models;

namespace TrackLink.Services;

public class TrackingService
{
    private readonly AppDbContext _context;

    public TrackingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tracking> Create(double latitude, double longitude)
    {
        var tracking = new Tracking
        {
            Token = Guid.NewGuid().ToString("N"),
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Trackings.Add(tracking);

        await _context.SaveChangesAsync();

        return tracking;
    }

    public async Task<Tracking?> GetByToken(string token)
    {
        return await _context.Trackings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public async Task<Tracking?> Update(
        string token,
        double latitude,
        double longitude)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(x => x.Token == token);

        if (tracking == null)
        {
            return null;
        }

        tracking.Latitude = latitude;
        tracking.Longitude = longitude;
        tracking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return tracking;
    }

    public async Task<bool> Delete(string token)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(x => x.Token == token);

        if (tracking == null)
        {
            return false;
        }

        _context.Trackings.Remove(tracking);

        await _context.SaveChangesAsync();

        return true;
    }
}