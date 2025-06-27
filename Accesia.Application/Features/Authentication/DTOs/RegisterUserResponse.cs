namespace Accesia.Application.Features.Authentication.DTOs;

public class RegisterUserResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public string? RedirectUrl { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}