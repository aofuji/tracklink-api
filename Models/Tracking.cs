namespace TrackLink.Models;

public class Tracking
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}