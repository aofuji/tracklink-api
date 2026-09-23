namespace TrackLink.Mcp.Responses;

public class GetMyTrackingsResponse
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<MyTrackingResponse> Trackings { get; set; } = [];
}