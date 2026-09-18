using Microsoft.AspNetCore.Mvc;
using TrackLink.DTOs;
using TrackLink.Services;
using TrackLink.Models;

namespace TrackLink.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly TrackingService _trackingService;

    public TrackingController(TrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateTrackingRequest request, CancellationToken cancellationToken)
    {
        var tracking = await _trackingService.CreateAsync(
            request.Latitude,
            request.Longitude,
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

    [HttpPut("{token}")]
    public async Task<IActionResult> UpdateAsync(
     string token,
     UpdateTrackingRequest request,
     CancellationToken cancellationToken)
    {
        var result = await _trackingService.UpdateAsync(
            token,
            request.Latitude,
            request.Longitude,
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

        return Ok(ToResponse(result.Tracking!));
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> DeleteAsync(string token, CancellationToken cancellationToken)
    {
        var deleted = await _trackingService.DeleteAsync(token, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

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
}