namespace Accesia.Application.Features.Users.DTOs;

public class ConfirmEmailChangeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NewEmail { get; set; }
    public UserProfileDto? Profile { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
} 