using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class Permission : AuditableEntity
{
    // Identificación básica
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }

    // Estructura del permiso
    public PermissionScope Scope { get; set; }

    // Metadatos
    public bool IsSystemPermission { get; set; }
    public bool IsActive { get; set; }

    // Navegación y relaciones
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Validación de condiciones
    public bool IsValidForUser(User user)
    {
        return IsActive;
    }
}
