using System.ComponentModel.DataAnnotations;

namespace TrackLink.DTOs;

public class UpdateTrackingRequest
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}