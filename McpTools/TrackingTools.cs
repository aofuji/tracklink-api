using ModelContextProtocol.Server;
using System.ComponentModel;
using TrackLink.Services;

namespace TrackLink.McpTools;

[McpServerToolType]
public class TrackingTools
{
    private readonly TrackingService _trackingService;

    public TrackingTools(TrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [McpServerTool]
    [Description("Checks whether the TrackLink MCP server is working.")]
    public string Ping()
    {
        return "TrackLink MCP is working!";
    }

    [McpServerTool(Name = "get_tracking_status")]
    [Description("Gets the current status and latest location of a TrackLink tracking session by its public token.")]
    public async Task<object> GetTrackingStatus(
    [Description("The public tracking token.")] string token,
    CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status != Services.GetTrackingStatus.Success)
        {
            return new
            {
                status = result.Status.ToString()
            };
        }

        var tracking = result.Tracking!;

        return new
        {
            status = result.Status.ToString(),
            tracking = new
            {
                tracking.Token,
                tracking.Latitude,
                tracking.Longitude,
                tracking.UpdatedAt,
                tracking.IsActive,
                tracking.ExpiresAt
            }
        };
    }

    [McpServerTool(Name = "get_tracking_history")]
    [Description("Gets the location history of a TrackLink tracking session by its public token.")]
    public async Task<object> GetTrackingHistory(
    [Description("The public tracking token.")] string token,
    CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetHistoryByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status != Services.GetTrackingStatus.Success)
        {
            return new
            {
                status = result.Status.ToString()
            };
        }

        return new
        {
            status = result.Status.ToString(),
            locations = result.Locations.Select(location => new
            {
                location.Latitude,
                location.Longitude,
                location.RecordedAt
            })
        };
    }

}