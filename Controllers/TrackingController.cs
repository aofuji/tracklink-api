using Microsoft.AspNetCore.Mvc;
using TrackLink.DTOs;
using TrackLink.Services;

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
    public async Task<IActionResult> Create(CreateTrackingRequest request)
    {
        var tracking = await _trackingService.Create(
            request.Latitude,
            request.Longitude
        );

        return CreatedAtAction(
            nameof(GetByToken),
            new { token = tracking.Token },
            tracking
        );
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetByToken(string token)
    {
        var tracking = await _trackingService.GetByToken(token);

        if (tracking == null)
        {
            return NotFound();
        }

        return Ok(tracking);
    }

    [HttpPut("{token}")]
    public async Task<IActionResult> Update(
      string token,
      UpdateTrackingRequest request)
    {
        var tracking = await _trackingService.Update(
            token,
            request.Latitude,
            request.Longitude
        );

        if (tracking == null)
        {
            return NotFound();
        }

        return Ok(tracking);
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Delete(string token)
    {
        var deleted = await _trackingService.Delete(token);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}