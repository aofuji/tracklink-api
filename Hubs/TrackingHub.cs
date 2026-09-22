using Microsoft.AspNetCore.SignalR;
using TrackLink.Services;

namespace TrackLink.Hubs;

public class TrackingHub : Hub
{
    private readonly TrackingService _trackingService;

    public TrackingHub(TrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    public async Task JoinTracking(string token)
    {
        var result = await _trackingService.GetByTokenAsync(
            token,
            Context.ConnectionAborted
        );

        if (result.Status != GetTrackingStatus.Success)
        {
            throw new HubException(
                "Tracking session is not available."
            );
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            token
        );
    }
}