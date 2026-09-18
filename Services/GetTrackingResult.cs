using TrackLink.Models;

namespace TrackLink.Services;

public enum GetTrackingStatus
{
    Success,
    NotFound,
    Inactive,
    Expired
}

public class GetTrackingResult
{
    public GetTrackingStatus Status { get; set; }

    public Tracking? Tracking { get; set; }
}