namespace Accesia.Application.Common.Exceptions;

public class AccountLockedException : Exception
{
    public string Email { get; }
    public DateTime LockedUntil { get; }
    public TimeSpan RemainingLockTime { get; }

    public AccountLockedException(string email, DateTime lockedUntil) 
        : base($"La cuenta {email} está bloqueada hasta {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC")
    {
        Email = email;
        LockedUntil = lockedUntil;
        RemainingLockTime = lockedUntil - DateTime.UtcNow;
    }

    public AccountLockedException(string email, DateTime lockedUntil, Exception innerException) 
        : base($"La cuenta {email} está bloqueada hasta {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC", innerException)
    {
        Email = email;
        LockedUntil = lockedUntil;
        RemainingLockTime = lockedUntil - DateTime.UtcNow;
    }
} 