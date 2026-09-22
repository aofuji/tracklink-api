using TrackLink.Models;

namespace TrackLink.Services;

public class GetTrackingHistoryResult
{
    public GetTrackingStatus Status { get; set; }

    public List<TrackingLocation> Locations { get; set; } = [];
}
