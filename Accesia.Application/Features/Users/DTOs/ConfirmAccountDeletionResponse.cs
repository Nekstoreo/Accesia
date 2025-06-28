namespace Accesia.Application.Features.Users.DTOs;

public class ConfirmAccountDeletionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
    public DateTime? PermanentDeletionDate { get; set; }
} 