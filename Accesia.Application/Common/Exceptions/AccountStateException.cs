using Accesia.Domain.Enums;

namespace Accesia.Application.Common.Exceptions;

public class AccountStateException : Exception
{
    public AccountStateException(string userId, UserStatus currentStatus, string message)
        : base(message)
    {
        UserId = userId;
        CurrentStatus = currentStatus;
    }

    public AccountStateException(string userId, UserStatus currentStatus, string message, Exception innerException)
        : base(message, innerException)
    {
        UserId = userId;
        CurrentStatus = currentStatus;
    }

    public UserStatus CurrentStatus { get; }
    public string UserId { get; }
}

public class AccountInactiveException : AccountStateException
{
    public AccountInactiveException(string userId)
        : base(userId, UserStatus.Inactive, "La cuenta está inactiva y requiere reactivación.")
    {
    }
}

public class AccountBlockedException : AccountStateException
{
    public AccountBlockedException(string userId, DateTime? lockedUntil = null)
        : base(userId, UserStatus.Blocked,
            lockedUntil.HasValue
                ? $"La cuenta está bloqueada hasta {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC"
                : "La cuenta está bloqueada")
    {
        LockedUntil = lockedUntil;
    }

    public DateTime? LockedUntil { get; }
}

public class AccountMarkedForDeletionException : AccountStateException
{
    public AccountMarkedForDeletionException(string userId)
        : base(userId, UserStatus.MarkedForDeletion,
            "La cuenta está marcada para eliminación y no puede realizar esta acción.")
    {
    }
}

public class InvalidStateTransitionException : AccountStateException
{
    public InvalidStateTransitionException(string userId, UserStatus currentStatus, UserStatus targetStatus)
        : base(userId, currentStatus,
            $"No se puede cambiar el estado de '{GetStatusDescription(currentStatus)}' a '{GetStatusDescription(targetStatus)}'.")
    {
        TargetStatus = targetStatus;
    }

    public UserStatus TargetStatus { get; }

    private static string GetStatusDescription(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "Activa",
            UserStatus.Inactive => "Inactiva",
            UserStatus.Blocked => "Bloqueada",
            UserStatus.PendingConfirmation => "Pendiente de confirmación",
            UserStatus.EmailPendingVerification => "Verificación de email pendiente",
            UserStatus.MarkedForDeletion => "Marcada para eliminación",
            _ => "Desconocido"
        };
    }
}