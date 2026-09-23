namespace TrackLink.Mcp.Responses;

public class TrackingHistoryResponse
{
    public string Status { get; set; } = string.Empty;

    public List<TrackingLocationResponse> Locations { get; set; } = [];
}