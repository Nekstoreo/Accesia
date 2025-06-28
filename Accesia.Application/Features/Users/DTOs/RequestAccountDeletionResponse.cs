namespace Accesia.Application.Features.Users.DTOs;

public class RequestAccountDeletionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? TokenExpiresAt { get; set; }
    public string EmailSent { get; set; } = string.Empty;
} 