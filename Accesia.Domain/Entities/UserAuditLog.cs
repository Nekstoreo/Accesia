using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class UserAuditLog : AuditableEntity
{
    // Constructor privado para EF Core
    private UserAuditLog()
    {
    }

    public UserAuditLog(Guid userId, ActionType actionType, ResourceType resourceType,
        string fieldName, string? oldValue, string? newValue,
        string ipAddress, string userAgent, string? reason = null)
    {
        UserId = userId;
        ActionType = actionType;
        ResourceType = resourceType;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ActionPerformedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; set; }
    public ActionType ActionType { get; set; }
    public ResourceType ResourceType { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime ActionPerformedAt { get; set; }

    // Propiedades de navegación
    public User User { get; set; } = null!;

    public static UserAuditLog CreateProfileUpdate(Guid userId, string fieldName,
        string? oldValue, string? newValue,
        string ipAddress, string userAgent,
        string? reason = null)
    {
        return new UserAuditLog(userId, ActionType.Update, ResourceType.UserProfile,
            fieldName, oldValue, newValue, ipAddress, userAgent, reason);
    }

    public static UserAuditLog CreateEmailChange(Guid userId, string oldEmail, string newEmail,
        string ipAddress, string userAgent,
        string? reason = null)
    {
        return new UserAuditLog(userId, ActionType.Update, ResourceType.UserProfile,
            "Email", oldEmail, newEmail, ipAddress, userAgent, reason);
    }
}