using Microsoft.AspNetCore.Mvc;
using TrackLink.DTOs;
using TrackLink.Services;
using TrackLink.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TrackLink.Hubs;

namespace TrackLink.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly TrackingService _trackingService;

    private readonly IHubContext<TrackingHub> _hubContext;

    public TrackingController(
     TrackingService trackingService,
     IHubContext<TrackingHub> hubContext)
    {
        _trackingService = trackingService;
        _hubContext = hubContext;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
    CreateTrackingRequest request,
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var tracking = await _trackingService.CreateAsync(
            request.Latitude,
            request.Longitude,
            userId,
            cancellationToken
        );

        return CreatedAtAction(
            "GetByToken",
            new { token = tracking.Token },
            ToResponse(tracking)
        );
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetByTokenAsync(
     string token,
     CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status == GetTrackingStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == GetTrackingStatus.Inactive)
        {
            return Conflict(new
            {
                message = "Tracking session is inactive."
            });
        }

        if (result.Status == GetTrackingStatus.Expired)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                message = "Tracking session has expired."
            });
        }

        return Ok(ToResponse(result.Tracking!));
    }

    [HttpGet("{token}/history")]
    public async Task<IActionResult> GetHistoryByTokenAsync(
     string token,
     CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetHistoryByTokenAsync(
            token,
            cancellationToken
        );

        if (result.Status == GetTrackingStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == GetTrackingStatus.Inactive)
        {
            return Conflict(new
            {
                message = "Tracking session is inactive."
            });
        }

        if (result.Status == GetTrackingStatus.Expired)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                message = "Tracking session has expired."
            });
        }

        var response = result.Locations
            .Select(ToLocationResponse)
            .ToList();

        return Ok(response);
    }

    [Authorize]
    [HttpPut("{token}")]
    public async Task<IActionResult> UpdateAsync(
    string token,
    UpdateTrackingRequest request,
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _trackingService.UpdateAsync(
            token,
            request.Latitude,
            request.Longitude,
            userId,
            cancellationToken
        );

        if (result.Status == UpdateTrackingStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == UpdateTrackingStatus.Inactive)
        {
            return Conflict(new
            {
                message = "Tracking session is inactive."
            });
        }

        if (result.Status == UpdateTrackingStatus.Expired)
        {
            return Conflict(new
            {
                message = "Tracking session has expired."
            });
        }

        var response = ToResponse(result.Tracking!);

        await _hubContext.Clients
            .Group(token)
            .SendAsync(
                "LocationUpdated",
                response,
                cancellationToken
            );

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{token}")]
    public async Task<IActionResult> DeleteAsync(
    string token,
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _trackingService.DeleteAsync(
            token,
            userId,
            cancellationToken
        );

        if (!deleted)
        {
            return NotFound();
        }

        await _hubContext.Clients
            .Group(token)
            .SendAsync(
                "TrackingEnded",
                cancellationToken
            );

        return NoContent();
    }

    private static TrackingResponse ToResponse(Tracking tracking)
    {
        return new TrackingResponse
        {
            Token = tracking.Token,
            Latitude = tracking.Latitude,
            Longitude = tracking.Longitude,
            UpdatedAt = tracking.UpdatedAt,
            IsActive = tracking.IsActive,
            ExpiresAt = tracking.ExpiresAt
        };
    }

    private static TrackingLocationResponse ToLocationResponse(
    TrackingLocation location)
    {
        return new TrackingLocationResponse
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            RecordedAt = location.RecordedAt
        };
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTrackingsAsync(
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var trackings = await _trackingService.GetByUserIdAsync(
            userId,
            cancellationToken
        );

        var response = trackings
            .Select(ToResponse)
            .ToList();

        return Ok(response);
    }

}
