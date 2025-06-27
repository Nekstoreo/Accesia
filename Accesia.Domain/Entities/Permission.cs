using System.Text.Json;
using Accesia.Domain.Common;
using Accesia.Domain.Enums;

namespace Accesia.Domain.Entities;

public class Permission : AuditableEntity
{
    // Identificación básica
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }

    // Estructura del permiso
    public ResourceType Resource { get; set; }
    public ActionType Action { get; set; }
    public PermissionScope Scope { get; set; }

    // Metadatos
    public bool IsSystemPermission { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresApproval { get; set; }
    public int RiskLevel { get; set; }

    // Condiciones opcionales
    public string? Conditions { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    
    // Navegación y relaciones
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Validación de condiciones
    public bool IsValidForUser(User user)
    {
        if (!IsActive || IsExpired())
            return false;

        if (!string.IsNullOrEmpty(Conditions))
        {
            try 
            {
                var conditionDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Conditions);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public bool IsExpired()
    {
        var now = DateTime.UtcNow;
        return (ValidFrom.HasValue && now < ValidFrom.Value) ||
               (ValidUntil.HasValue && now > ValidUntil.Value);
    }

    public bool MatchesResource(string resource, string action)
    {
        return Resource.ToString().Equals(resource, StringComparison.OrdinalIgnoreCase) &&
               Action.ToString().Equals(action, StringComparison.OrdinalIgnoreCase);
    }

    // Factory methods
    public static Permission CreateSystemPermission(string name, ResourceType resource, ActionType action)
    {
        return new Permission
        {
            Name = name,
            DisplayName = name,
            Description = name,
            Category = "System",
            Resource = resource,
            Action = action,
            IsSystemPermission = true,
            IsActive = true,
            Scope = PermissionScope.Global,
            RiskLevel = 1
        };
    }

    public static Permission CreateCustomPermission(
        string name, 
        string description, 
        ResourceType resource, 
        ActionType action, 
        PermissionScope scope)
    {
        return new Permission
        {
            Name = name,
            DisplayName = name,
            Description = description,
            Category = "Custom",
            Resource = resource,
            Action = action,
            Scope = scope,
            IsSystemPermission = false,
            IsActive = true,
            RequiresApproval = false,
            RiskLevel = 5
        };
    }

    // Utilidades
    public string GetFullPermissionString()
    {
        return $"{Resource}:{Action}:{Scope}";
    }

    public (ResourceType Resource, ActionType Action, PermissionScope Scope) GetPermissionParts()
    {
        return (Resource, Action, Scope);
    }
}
