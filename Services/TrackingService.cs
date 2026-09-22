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

    public async Task<Tracking> CreateAsync(double latitude, double longitude, int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var tracking = new Tracking
        {
            Token = Guid.NewGuid().ToString("N"),
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = now,
            IsActive = true,
            ExpiresAt = now.AddHours(24),
            UserId = userId
        };

        _context.Trackings.Add(tracking);

        await _context.SaveChangesAsync(cancellationToken);

        return tracking;
    }

    public async Task<GetTrackingResult> GetByTokenAsync(
     string token,
     CancellationToken cancellationToken)
    {
        var tracking = await _context.Trackings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Token == token,
                cancellationToken
            );

        if (tracking == null)
        {
            return new GetTrackingResult
            {
                Status = GetTrackingStatus.NotFound
            };
        }

        if (!tracking.IsActive)
        {
            return new GetTrackingResult
            {
                Status = GetTrackingStatus.Inactive
            };
        }

        if (tracking.ExpiresAt <= DateTime.UtcNow)
        {
            return new GetTrackingResult
            {
                Status = GetTrackingStatus.Expired
            };
        }

        return new GetTrackingResult
        {
            Status = GetTrackingStatus.Success,
            Tracking = tracking
        };
    }

    public async Task<UpdateTrackingResult> UpdateAsync(
      string token,
      double latitude,
      double longitude,
      int userId,
      CancellationToken cancellationToken)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(
                x => x.Token == token && x.UserId == userId,
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

        if (tracking.ExpiresAt <= DateTime.UtcNow)
        {
            return new UpdateTrackingResult
            {
                Status = UpdateTrackingStatus.Expired
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
     int userId,
     CancellationToken cancellationToken)
    {
        var tracking = await _context.Trackings
            .FirstOrDefaultAsync(
                x => x.Token == token && x.UserId == userId,
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

    public async Task<List<Tracking>> GetByUserIdAsync(
    int userId,
    CancellationToken cancellationToken)
    {
        return await _context.Trackings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}