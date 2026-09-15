using TrackLink.Models;

namespace TrackLink.Services;

public class TrackingService
{
    private readonly List<Tracking> _trackings = new();

    private int _nextId = 1;

    public Tracking Create(double latitude, double longitude)
    {
        var tracking = new Tracking
        {
            Id = _nextId++,
            Token = Guid.NewGuid().ToString("N"),
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = DateTime.UtcNow
        };

        _trackings.Add(tracking);

        return tracking;
    }

    public Tracking? GetByToken(string token)
    {
        return _trackings.FirstOrDefault(x => x.Token == token);
    }

    public Tracking? Update(
    string token,
    double latitude,
    double longitude)
    {
        var tracking = _trackings.FirstOrDefault(x => x.Token == token);

        if (tracking == null)
        {
            return null;
        }

        tracking.Latitude = latitude;
        tracking.Longitude = longitude;
        tracking.UpdatedAt = DateTime.UtcNow;

        return tracking;
    }

    public bool Delete(string token)
    {
        var tracking = _trackings.FirstOrDefault(x => x.Token == token);

        if (tracking == null)
        {
            return false;
        }

        _trackings.Remove(tracking);

        return true;
    }
}