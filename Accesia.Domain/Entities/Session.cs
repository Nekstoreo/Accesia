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

    // Información del dispositivo/ubicación
    public required DeviceInfo DeviceInfo { get; set; }
    public required LocationInfo LocationInfo { get; set; }
    public bool IsKnownDevice { get; set; }
    public string? DeviceName { get; set; }

    // Metadatos de seguridad
    public LoginMethod LoginMethod { get; set; }
    public bool MfaVerified { get; set; }
    public bool TwoFactorRequired { get; set; }
    public int RiskScore { get; set; }

    // Auditoría adicional
    public required string UserAgent { get; set; }
    public required string InitialIpAddress { get; set; }
    public required string LastIpAddress { get; set; }

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

    public void Invalidate(string reason)
    {
        Status = SessionStatus.Invalidated;
        LastActivityAt = DateTime.UtcNow;
        // Potencialmente registrar la razón de invalidación
    }

    public void MarkAsSuspicious(string reason)
    {
        Status = SessionStatus.Suspended;
        RiskScore += 10; // Incrementar puntuación de riesgo
        LastActivityAt = DateTime.UtcNow;
        // Potencialmente registrar la razón de sospecha
    }

    // Gestión de actividad
    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    public void UpdateLocation(LocationInfo newLocation)
    {
        LocationInfo = newLocation;
        LastIpAddress = newLocation.IpAddress;
        IsKnownDevice = false; // Marcar como dispositivo no conocido si la ubicación cambia
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

    public bool ShouldRequireMfa()
    {
        return !MfaVerified || 
               (TwoFactorRequired && RiskScore > 50);
    }

    // Gestión de tokens
    public void GenerateNewRefreshToken()
    {
        RefreshToken = Guid.NewGuid().ToString();
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7); // Ejemplo de expiración
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = string.Empty;
        RefreshTokenExpiresAt = DateTime.UtcNow;
    }

    // Métodos de creación de sesiones
    public static Session CreateNewSession(User user, DeviceInfo device, LocationInfo location, string loginMethod)
    {
        return new Session
        {
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            Status = SessionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 horas por defecto
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            LastActivityAt = DateTime.UtcNow,
            DeviceInfo = device,
            LocationInfo = location,
            IsKnownDevice = false,
            LoginMethod = ParseLoginMethod(loginMethod),
            MfaVerified = false,
            TwoFactorRequired = false,
            RiskScore = 0,
            UserAgent = device.UserAgent,
            InitialIpAddress = location.IpAddress,
            LastIpAddress = location.IpAddress
        };
    }

    public static Session CreateFromOAuth(User user, DeviceInfo device, LocationInfo location, string provider)
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
            DeviceInfo = device,
            LocationInfo = location,
            IsKnownDevice = false,
            LoginMethod = LoginMethod.GoogleOAuth,
            MfaVerified = false, // OAuth puede requerir MFA adicional
            TwoFactorRequired = false,
            RiskScore = 0,
            UserAgent = device.UserAgent,
            InitialIpAddress = location.IpAddress,
            LastIpAddress = location.IpAddress
        };
    }

    public static Session CreateMfaSession(User user, DeviceInfo device, LocationInfo location)
    {
        return new Session
        {
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            Status = SessionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Sesión MFA más corta
            RefreshTokenExpiresAt = DateTime.UtcNow.AddHours(2),
            LastActivityAt = DateTime.UtcNow,
            DeviceInfo = device,
            LocationInfo = location,
            IsKnownDevice = false,
            LoginMethod = LoginMethod.MFA,
            MfaVerified = true,
            TwoFactorRequired = false,
            RiskScore = 0,
            UserAgent = device.UserAgent,
            InitialIpAddress = location.IpAddress,
            LastIpAddress = location.IpAddress
        };
    }

    private static LoginMethod ParseLoginMethod(string loginMethod)
    {
        return loginMethod.ToLowerInvariant() switch
        {
            "password" => LoginMethod.Password,
            "oauth" => LoginMethod.GoogleOAuth,
            "mfa" => LoginMethod.MFA,
            "sso" => LoginMethod.SAML,
            _ => LoginMethod.Password
        };
    }
}