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

    public async Task<Tracking> CreateAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var tracking = new Tracking
        {
            Token = Guid.NewGuid().ToString("N"),
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Trackings.Add(tracking);

        await _context.SaveChangesAsync(cancellationToken);

        return tracking;
    }

    public async Task<Tracking?> GetByTokenAsync(
     string token,
     CancellationToken cancellationToken)
    {
        return await _context.Trackings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Token == token,
                cancellationToken
            );
    }

    public async Task<UpdateTrackingResult> UpdateAsync(
    string token,
    double latitude,
    double longitude,
    CancellationToken cancellationToken)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(
                x => x.Token == token,
                cancellationToken
            );

        if (tracking == null)
        {
            return new UpdateTrackingResult
            {
                Status = UpdateTrackingStatus.NotFound
            };
        }

        if (!tracking.IsActive)
        {
            return new UpdateTrackingResult
            {
                Status = UpdateTrackingStatus.Inactive
            };
        }

        tracking.Latitude = latitude;
        tracking.Longitude = longitude;
        tracking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateTrackingResult
        {
            Status = UpdateTrackingStatus.Success,
            Tracking = tracking
        };
    }

    public async Task<bool> DeleteAsync(
      string token,
      CancellationToken cancellationToken)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(
                x => x.Token == token,
                cancellationToken
            );

        if (tracking == null)
        {
            return false;
        }

        tracking.IsActive = false;
        tracking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}