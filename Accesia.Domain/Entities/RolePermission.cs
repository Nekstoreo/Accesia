using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class RolePermission : AuditableEntity
{
    // Relación many-to-many
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    
    // Metadatos de la asignación
    public DateTime GrantedAt { get; set; }
    public Guid GrantedBy { get; set; }
    public bool IsInherited { get; set; }
    public string? Conditions { get; set; } // JSON con condiciones específicas
    public bool IsActive { get; set; }
    
    // Propiedades de navegación
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
    public User GrantedByUser { get; set; } = null!;
    
    // Constructor privado para EF Core
    private RolePermission() { }
    
    // Constructor público
    public RolePermission(Guid roleId, Guid permissionId, Guid grantedBy, string? conditions = null)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedBy = grantedBy;
        GrantedAt = DateTime.UtcNow;
        IsInherited = false;
        IsActive = true;
        Conditions = conditions;
    }
    
    // Factory method para permisos heredados
    public static RolePermission CreateInheritedPermission(Guid roleId, Guid permissionId, Guid inheritedFrom)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedBy = inheritedFrom,
            GrantedAt = DateTime.UtcNow,
            IsInherited = true,
            IsActive = true
        };
    }
    
    // Métodos de utilidad
    public bool IsValidCondition()
    {
        if (string.IsNullOrEmpty(Conditions))
            return true;
            
        try
        {
            // Validar que el JSON sea válido
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Conditions);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public void Revoke()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}