namespace TrackLink.DTOs;

public class TrackingResponse
{
    public string Token { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }
    public DateTime ExpiresAt { get; set; }
}