namespace Accesia.Domain.Entities;

public enum UserStatus
{
    // Estados: Active, Inactive, Pending, Blocked, EmailPendingVerification, MarkedForDeletion
    Active = 1,
    Inactive = 2,
    Pending = 3,
    PendingConfirmation = 7,
    Blocked = 4,
    EmailPendingVerification = 5,
    MarkedForDeletion = 6
}