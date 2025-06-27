using System.ComponentModel.DataAnnotations;
namespace Accesia.Application.Features.Authentication.DTOs;

public class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; } = string.Empty;
    [Required]
    [MinLength(8)]
    public required string Password { get; set; } = string.Empty;
    [Required]
    [Compare("Password")]
    public required string ConfirmPassword { get; set; } = string.Empty;
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; } = string.Empty;
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Bogota";
}