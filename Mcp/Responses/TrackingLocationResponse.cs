namespace TrackLink.Mcp.Responses;

public class TrackingLocationResponse
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime RecordedAt { get; set; }
}