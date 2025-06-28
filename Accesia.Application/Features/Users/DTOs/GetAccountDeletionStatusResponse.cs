namespace Accesia.Application.Features.Users.DTOs;

public class GetAccountDeletionStatusResponse
{
    public bool IsMarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionAt { get; set; }
    public DateTime? PermanentDeletionDate { get; set; }
    public string? DeletionReason { get; set; }
    public bool IsInGracePeriod { get; set; }
    public int DaysRemainingInGracePeriod { get; set; }
    public bool HasPendingDeletionRequest { get; set; }
    public DateTime? DeletionTokenExpiresAt { get; set; }
} 