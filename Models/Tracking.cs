namespace TrackLink.Models;

public class Tracking
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime UpdatedAt { get; set; }
}