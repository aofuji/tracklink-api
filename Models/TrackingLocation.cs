namespace TrackLink.Models;

public class TrackingLocation
{
    public int Id { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime RecordedAt { get; set; }

    public int TrackingId { get; set; }

    public Tracking Tracking { get; set; } = null!;
}
