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
        var tracking = await _trackingService.GetByTokenAsync(
            token,
            cancellationToken
        );

        if (tracking == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(tracking));
    }

    [HttpPut("{token}")]
    public async Task<IActionResult> UpdateAsync(
      string token,
      UpdateTrackingRequest request,
      CancellationToken cancellationToken)
    {
        var tracking = await _trackingService.UpdateAsync(
            token,
            request.Latitude,
            request.Longitude,
            cancellationToken
        );

        if (tracking == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(tracking));
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
            UpdatedAt = tracking.UpdatedAt
        };
    }
}