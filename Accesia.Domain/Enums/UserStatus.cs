namespace Accesia.Domain.Entities;

public enum UserStatus
{
    // Estados: Active, Inactive, Pending, Blocked, EmailPendingVerification, MarkedForDeletion
    Active = 1,
    Inactive = 2,
    Pending = 3,
    PendingConfirmation = 4,
    Blocked = 5,
    EmailPendingVerification = 6,
    MarkedForDeletion = 7
}