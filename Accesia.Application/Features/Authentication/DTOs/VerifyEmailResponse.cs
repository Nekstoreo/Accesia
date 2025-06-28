namespace Accesia.Application.Features.Authentication.DTOs;

public class VerifyEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? EmailVerifiedAt { get; set; }
    public bool IsAccountActivated { get; set; }
    public string? RedirectUrl { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}