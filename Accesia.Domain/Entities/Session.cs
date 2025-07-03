using Accesia.Domain.Common;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;

namespace Accesia.Domain.Entities;

public class Session : AuditableEntity
{
    // Identificación y relación
    public Guid UserId { get; set; }
    public required string SessionToken { get; set; }
    public required string RefreshToken { get; set; }
    public SessionStatus Status { get; set; }

    // Información temporal
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Metadatos de seguridad
    public LoginMethod LoginMethod { get; set; }
    public bool TwoFactorVerified { get; set; }
    public User User { get; set; } = null!;

    // Gestión de estado
    public void Activate()
    {
        if (Status != SessionStatus.Active)
        {
            Status = SessionStatus.Active;
            LastActivityAt = DateTime.UtcNow;
        }
    }

    public void Expire()
    {
        Status = SessionStatus.Expired;
        LastActivityAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        Status = SessionStatus.Revoked;
        LastActivityAt = DateTime.UtcNow;
    }

    public void Invalidate()
    {
        Status = SessionStatus.Invalidated;
        LastActivityAt = DateTime.UtcNow;
    }

    // Gestión de actividad
    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }


    public void ExtendExpiration(TimeSpan extension)
    {
        if (Status == SessionStatus.Active)
        {
            ExpiresAt = ExpiresAt.Add(extension);
            RefreshTokenExpiresAt = RefreshTokenExpiresAt.Add(extension);
        }
    }

    // Validaciones
    public bool IsActive()
    {
        return Status == SessionStatus.Active && 
               DateTime.UtcNow < ExpiresAt;
    }

    public bool IsExpired()
    {
        return Status == SessionStatus.Expired || 
               DateTime.UtcNow >= ExpiresAt;
    }

    public bool CanBeRefreshed()
    {
        return Status == SessionStatus.Active && 
               DateTime.UtcNow < RefreshTokenExpiresAt;
    }

    // Gestión de tokens
    public void GenerateNewRefreshToken()
    {
        RefreshToken = Guid.NewGuid().ToString();
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = string.Empty;
        RefreshTokenExpiresAt = DateTime.UtcNow;
    }

    public static Session CreateNewSession(User user, string loginMethod)
    {
        return new Session
        {
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            Status = SessionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            LastActivityAt = DateTime.UtcNow,
            LoginMethod = loginMethod.ToLowerInvariant() switch
            {
                "password" => LoginMethod.Password,
                "2fa" => LoginMethod.TwoFactor,
                _ => LoginMethod.Password
            },
            TwoFactorVerified = false,
        };
    }
}