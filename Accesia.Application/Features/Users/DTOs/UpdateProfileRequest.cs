using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "es";
    
    [MaxLength(50)]
    public string TimeZone { get; set; } = "America/Bogota";
} 