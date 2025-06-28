using Accesia.Domain.Common;
using Accesia.Domain.ValueObjects;

namespace Accesia.Domain.Entities;

public class SecurityAuditLog : AuditableEntity
{
    // Constructor privado para EF Core
    private SecurityAuditLog()
    {
    }

    public SecurityAuditLog(
        Guid? userId,
        string eventType,
        string eventCategory,
        string description,
        string ipAddress,
        string userAgent,
        DeviceInfo deviceInfo,
        string endpoint,
        string httpMethod,
        bool isSuccessful,
        string severity = "Medium",
        string? requestId = null,
        string? failureReason = null,
        int? responseStatusCode = null,
        LocationInfo? locationInfo = null,
        Dictionary<string, object>? additionalData = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EventType = eventType;
        EventCategory = eventCategory;
        Description = description;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DeviceInfo = deviceInfo;
        LocationInfo = locationInfo;
        Endpoint = endpoint;
        HttpMethod = httpMethod;
        RequestId = requestId;
        IsSuccessful = isSuccessful;
        FailureReason = failureReason;
        ResponseStatusCode = responseStatusCode;
        AdditionalData = additionalData ?? new Dictionary<string, object>();
        Severity = severity;
        OccurredAt = DateTime.UtcNow;
    }

    public new Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventCategory { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public DeviceInfo DeviceInfo { get; private set; } = null!;
    public LocationInfo? LocationInfo { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = string.Empty;
    public string? RequestId { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string? FailureReason { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public Dictionary<string, object> AdditionalData { get; } = new();
    public string Severity { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    // Propiedades de navegación
    public User? User { get; set; }

    // Factory methods para eventos específicos
    public static SecurityAuditLog CreateLoginAttempt(
        Guid? userId, string email, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["email"] = email,
            ["attemptedUserId"] = userId?.ToString() ?? "unknown"
        };

        return new SecurityAuditLog(
            userId, "LoginAttempt", "Authentication",
            isSuccessful ? $"Login exitoso para {email}" : $"Login fallido para {email}: {failureReason}",
            ipAddress, userAgent, deviceInfo, "/api/auth/login", "POST",
            isSuccessful, isSuccessful ? "Low" : "High",
            failureReason: failureReason, locationInfo: locationInfo,
            additionalData: additionalData);
    }

    public static SecurityAuditLog CreatePasswordChange(
        Guid userId, string ipAddress, string userAgent, DeviceInfo deviceInfo,
        bool isSuccessful, string? failureReason = null, LocationInfo? locationInfo = null)
    {
        return new SecurityAuditLog(
            userId, "PasswordChange", "AccountSecurity",
            isSuccessful ? "Contraseña cambiada exitosamente" : $"Cambio de contraseña fallido: {failureReason}",
            ipAddress, userAgent, deviceInfo, "/api/auth/change-password", "POST",
            isSuccessful, "High", failureReason: failureReason, locationInfo: locationInfo);
    }

    public static SecurityAuditLog CreateEmailChange(
        Guid userId, string oldEmail, string newEmail, string ipAddress,
        string userAgent, DeviceInfo deviceInfo, bool isSuccessful,
        string? failureReason = null, LocationInfo? locationInfo = null)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["oldEmail"] = oldEmail,
            ["newEmail"] = newEmail
        };

        return new SecurityAuditLog(
            userId, "EmailChange", "AccountSecurity",
            isSuccessful ? $"Email cambiado de {oldEmail} a {newEmail}" : $"Cambio de email fallido: {failureReason}",
            ipAddress, userAgent, deviceInfo, "/api/users/change-email", "POST",
            isSuccessful, "High", failureReason: failureReason,
            locationInfo: locationInfo, additionalData: additionalData);
    }

    public static SecurityAuditLog CreateAccountDeletion(
        Guid userId, string ipAddress, string userAgent, DeviceInfo deviceInfo,
        bool isSuccessful, string? failureReason = null, LocationInfo? locationInfo = null)
    {
        return new SecurityAuditLog(
            userId, "AccountDeletion", "AccountSecurity",
            isSuccessful
                ? "Solicitud de eliminación de cuenta procesada"
                : $"Solicitud de eliminación fallida: {failureReason}",
            ipAddress, userAgent, deviceInfo, "/api/users/request-account-deletion", "POST",
            isSuccessful, "Critical", failureReason: failureReason, locationInfo: locationInfo);
    }

    public static SecurityAuditLog CreateRateLimitExceeded(
        Guid? userId, string ipAddress, string userAgent, DeviceInfo deviceInfo,
        string endpoint, string policyName, LocationInfo? locationInfo = null)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["policyName"] = policyName,
            ["limitType"] = "RateLimit"
        };

        return new SecurityAuditLog(
            userId, "RateLimitExceeded", "Security",
            $"Rate limit excedido para política {policyName} en endpoint {endpoint}",
            ipAddress, userAgent, deviceInfo, endpoint, "ANY",
            false, failureReason: $"Rate limit policy '{policyName}' exceeded",
            responseStatusCode: 429, locationInfo: locationInfo, additionalData: additionalData);
    }

    public static SecurityAuditLog CreateSuspiciousActivity(
        Guid? userId, string ipAddress, string userAgent, DeviceInfo deviceInfo,
        string endpoint, string activityDescription, string? failureReason = null,
        LocationInfo? locationInfo = null, Dictionary<string, object>? additionalData = null)
    {
        return new SecurityAuditLog(
            userId, "SuspiciousActivity", "Security",
            $"Actividad sospechosa detectada: {activityDescription}",
            ipAddress, userAgent, deviceInfo, endpoint, "ANY",
            false, "Critical", failureReason: failureReason,
            locationInfo: locationInfo, additionalData: additionalData);
    }

    public static SecurityAuditLog CreateUnauthorizedAccess(
        Guid? userId, string ipAddress, string userAgent, DeviceInfo deviceInfo,
        string endpoint, string httpMethod, string? failureReason = null,
        LocationInfo? locationInfo = null)
    {
        return new SecurityAuditLog(
            userId, "UnauthorizedAccess", "Security",
            $"Intento de acceso no autorizado a {endpoint}",
            ipAddress, userAgent, deviceInfo, endpoint, httpMethod,
            false, "High", failureReason: failureReason,
            responseStatusCode: 401, locationInfo: locationInfo);
    }

    // Métodos para agregar datos adicionales
    public void AddAdditionalData(string key, object value)
    {
        AdditionalData[key] = value;
    }

    public void AddAdditionalData(Dictionary<string, object> data)
    {
        foreach (var kvp in data) AdditionalData[kvp.Key] = kvp.Value;
    }
}