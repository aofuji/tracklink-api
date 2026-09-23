namespace TrackLink.Mcp.Responses;

public class TrackingStatusResponse
{
    public string Status { get; set; } = string.Empty;

    public MyTrackingResponse? Tracking { get; set; }
}