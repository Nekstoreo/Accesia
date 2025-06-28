using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Accesia.Domain.Common;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;

namespace Accesia.Domain.Entities;

public class User : AuditableEntity
{
    // Información básica del usuario
    [Required]
    [EmailAddress]
    public required Email Email { get; set; }
    [Required]
    public required string PasswordHash { get; set; }
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    [Required]
    [EnumDataType(typeof(UserStatus))]
    public UserStatus Status { get; set; }

    // Fechas importantes
    public DateTime? LastLoginAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime PasswordChangedAt { get; set; }

    // Configuraciones de seguridad
    public bool IsEmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Metadatos opcionales
    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Bogota";

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Constructor privado para EF Core
    private User() { }

    [SetsRequiredMembers]
    public User(Email email, string passwordHash, string firstName, string lastName)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.PendingConfirmation;
        IsEmailVerified = false;
        PasswordChangedAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
    }

    // Factory method estático CreateNewUser()
    public static User CreateNewUser(Email email, string passwordHash, string firstName, string lastName)
    {
        return new User(email, passwordHash, firstName, lastName);
    }

    // Métodos de validación
    public void VerifyEmail()
    {
        if (IsEmailVerified)
            throw new InvalidOperationException("El email ya está verificado.");

        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
    }

    public void LockAccount(TimeSpan duration)
    {
        LockedUntil = DateTime.UtcNow + duration;
        Status = UserStatus.Blocked;
    }

    public void UnlockAccount()
    {
        LockedUntil = null;
        Status = UserStatus.Active;
        ResetFailedLoginAttempts();
    }

    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        
        // Bloqueo exponencial basado en número de intentos
        if (FailedLoginAttempts >= GetMaxFailedAttempts())
        {
            var lockoutDuration = CalculateExponentialLockoutDuration();
            LockAccount(lockoutDuration);
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
    }

    public void OnSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        ResetFailedLoginAttempts();
        
        if (Status == UserStatus.Blocked && !IsAccountLocked())
        {
            Status = UserStatus.Active;
        }
    }

    private int GetMaxFailedAttempts()
    {
        return 5; // Configurable en el futuro
    }

    private TimeSpan CalculateExponentialLockoutDuration()
    {
        // Fórmula exponencial: 2^(intentos - maxIntentos) minutos
        var extraAttempts = FailedLoginAttempts - GetMaxFailedAttempts();
        var minutes = Math.Pow(2, extraAttempts + 1); // Comienza en 2 minutos
        
        // Limitar a un máximo de 24 horas
        var maxMinutes = 24 * 60; // 24 horas
        minutes = Math.Min(minutes, maxMinutes);
        
        return TimeSpan.FromMinutes(minutes);
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
    }

    public void SetEmailVerificationToken(string token, DateTime expiresAt)
    {
        EmailVerificationToken = token;
        EmailVerificationTokenExpiresAt = expiresAt;
    }

    public bool IsAccountLocked()
    {
        return LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
    }

    public bool CanAttemptLogin()
    {
        return !IsAccountLocked() && FailedLoginAttempts < 5 && Status == UserStatus.Active;
    }

    public bool HasPermission(string permissionName)
    {
        return UserRoles
            .Where(ur => ur.IsActive)
            .Any(ur => ur.Role.GetEffectivePermissions()
                .Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase)));
    }

    public IEnumerable<Permission> GetEffectivePermissions()
    {
        return UserRoles
            .Where(ur => ur.IsActive)
            .SelectMany(ur => ur.Role.GetEffectivePermissions())
            .Distinct();
    }

    public bool HasRole(string roleName)
    {
        return UserRoles
            .Where(ur => ur.IsActive)
            .Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }  
}