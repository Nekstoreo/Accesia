namespace Accesia.Application.Features.Users.DTOs;

public class UpdateProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserProfileDto? Profile { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
} 