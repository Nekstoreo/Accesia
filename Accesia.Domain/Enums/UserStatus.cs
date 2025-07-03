namespace Accesia.Domain.Entities;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Pending = 3,
    PendingConfirmation = 7,
    Blocked = 4,
    MarkedForDeletion = 5
}