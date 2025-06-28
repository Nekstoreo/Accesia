namespace Accesia.Application.Features.Users.DTOs;

public class CancelAccountDeletionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? RestoredAt { get; set; }
}