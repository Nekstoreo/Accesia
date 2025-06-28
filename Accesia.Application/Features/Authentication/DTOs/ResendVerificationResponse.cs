namespace Accesia.Application.Features.Authentication.DTOs;

public class ResendVerificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? TokenExpiresAt { get; set; }
    public bool WasTokenRefreshed { get; set; }
    public TimeSpan? NextResendAllowedIn { get; set; }
    public DateTime NextResendAllowedAt { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
