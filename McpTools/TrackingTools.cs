using ModelContextProtocol.Server;
using System.ComponentModel;
using TrackLink.Services;
using System.Security.Claims;
using TrackLink.Mcp.Responses;
using Microsoft.AspNetCore.SignalR;
using TrackLink.Hubs;

namespace TrackLink.McpTools;

[McpServerToolType]
public class TrackingTools
{
    private readonly TrackingService _trackingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IHubContext<TrackingHub> _hubContext;

    public TrackingTools(
     TrackingService trackingService,
     IHttpContextAccessor httpContextAccessor,
     IHubContext<TrackingHub> hubContext)
    {
        _trackingService = trackingService;
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
    }

    [McpServerTool]
    [Description("Checks whether the TrackLink MCP server is working.")]
    public string Ping()
    {
        return "TrackLink MCP is working!";
    }

    [McpServerTool(Name = "get_tracking_status")]
    [Description("Gets the current status and location of a tracking session by its public token.")]
    public async Task<TrackingStatusResponse> GetTrackingStatus(
    [Description("The public tracking token.")] string token,
    CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status != TrackLink.Services.GetTrackingStatus.Success)
        {
            return new TrackingStatusResponse
            {
                Status = result.Status.ToString()
            };
        }

        var tracking = result.Tracking!;

        return new TrackingStatusResponse
        {
            Status = result.Status.ToString(),
            Tracking = new MyTrackingResponse
            {
                Token = tracking.Token,
                Latitude = tracking.Latitude,
                Longitude = tracking.Longitude,
                UpdatedAt = tracking.UpdatedAt,
                IsActive = tracking.IsActive,
                ExpiresAt = tracking.ExpiresAt
            }
        };
    }

    [McpServerTool(Name = "get_tracking_history")]
    [Description("Gets the location history of a tracking session by its public token.")]
    public async Task<TrackingHistoryResponse> GetTrackingHistory(
    [Description("The public tracking token.")] string token,
    CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetHistoryByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status != TrackLink.Services.GetTrackingStatus.Success)
        {
            return new TrackingHistoryResponse
            {
                Status = result.Status.ToString()
            };
        }

        return new TrackingHistoryResponse
        {
            Status = result.Status.ToString(),

            Locations = result.Locations
                .Select(location => new TrackingLocationResponse
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    RecordedAt = location.RecordedAt
                })
                .ToList()
        };
    }

    [McpServerTool(Name = "who_am_i")]
    [Description("Returns information about the currently authenticated TrackLink user.")]
    public WhoAmIResponse WhoAmI()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user?.FindFirstValue(ClaimTypes.Name);
        var email = user?.FindFirstValue(ClaimTypes.Email);

        return new WhoAmIResponse
        {
            UserId = userId,
            Name = name,
            Email = email
        };
    }

    [McpServerTool(Name = "get_my_trackings")]
    [Description("Gets all tracking sessions belonging to the currently authenticated user.")]
    public async Task<GetMyTrackingsResponse> GetMyTrackings(
    CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return new GetMyTrackingsResponse
            {
                Success = false,
                Error = "Authenticated user could not be identified."
            };
        }

        var trackings = await _trackingService.GetByUserIdAsync(
            userId,
            cancellationToken
        );

        return new GetMyTrackingsResponse
        {
            Success = true,
            Trackings = trackings
                .Select(tracking => new MyTrackingResponse
                {
                    Token = tracking.Token,
                    Latitude = tracking.Latitude,
                    Longitude = tracking.Longitude,
                    UpdatedAt = tracking.UpdatedAt,
                    IsActive = tracking.IsActive,
                    ExpiresAt = tracking.ExpiresAt
                })
                .ToList()
        };
    }

    [McpServerTool(Name = "stop_tracking")]
    [Description("Stops a tracking session belonging to the currently authenticated user.")]
    public async Task<StopTrackingResponse> StopTracking(
    [Description("The public tracking token.")] string token,
    CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return new StopTrackingResponse
            {
                Success = false,
                Error = "Authenticated user could not be identified."
            };
        }

        var success = await _trackingService.DeleteAsync(
            token,
            userId,
            cancellationToken
        );

        if (!success)
        {
            return new StopTrackingResponse
            {
                Success = false,
                Error = "Tracking session was not found."
            };
        }

        await _hubContext.Clients
            .Group(token)
            .SendAsync(
                "TrackingEnded",
                cancellationToken
            );

        return new StopTrackingResponse
        {
            Success = true
        };
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var userIdClaim = _httpContextAccessor
            .HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out userId);
    }

}