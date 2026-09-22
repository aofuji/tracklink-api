using System.ComponentModel.DataAnnotations;

namespace TrackLink.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}