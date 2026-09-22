using System.ComponentModel.DataAnnotations;

namespace TrackLink.DTOs;

public class RegisterRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}