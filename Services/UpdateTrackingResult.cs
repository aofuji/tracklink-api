using TrackLink.Models;
namespace TrackLink.Services;

public enum UpdateTrackingStatus
{
    Success,
    NotFound,
    Inactive
}

public class UpdateTrackingResult
{
    public UpdateTrackingStatus Status { get; set; }

    public Tracking? Tracking { get; set; }
}