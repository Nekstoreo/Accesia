namespace Accesia.Application.Features.Authentication.DTOs;

public class UserRegistrationDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Bogota";
    public string HashedPassword { get; set; } = string.Empty;
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime EmailVerificationTokenExpiresAt { get; set; }
}
