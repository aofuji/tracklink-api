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
    public IActionResult Create(CreateTrackingRequest request)
    {
        var tracking = _trackingService.Create(
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
    public IActionResult GetByToken(string token)
    {
        var tracking = _trackingService.GetByToken(token);

        if (tracking == null)
        {
            return NotFound();
        }

        return Ok(tracking);
    }

    [HttpPut("{token}")]
    public IActionResult Update(
    string token,
    UpdateTrackingRequest request)
    {
        var tracking = _trackingService.Update(
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
    public IActionResult Delete(string token)
    {
        var deleted = _trackingService.Delete(token);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}