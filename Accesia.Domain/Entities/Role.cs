using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class Role : AuditableEntity
{
    // Identificación básica
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }

    // Configuración del rol
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }

    // Navegación y relaciones
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Gestión de permisos
    public void AddPermission(Permission permission, Guid grantedBy, string? conditions = null)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        var existingRelation = RolePermissions.FirstOrDefault(rp => rp.PermissionId == permission.Id && rp.IsActive);
        if (existingRelation == null)
        {
            var rolePermission = new RolePermission(Id, permission.Id, grantedBy, conditions);
            RolePermissions.Add(rolePermission);
        }
    }

    public void RemovePermission(Guid permissionId)
    {
        var rolePermission = RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId && rp.IsActive);
        if (rolePermission != null)
        {
            rolePermission.Revoke();
        }
    }

    public bool HasPermission(string permissionName)
    {
        return RolePermissions
            .Where(rp => rp.IsActive)
            .Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Permission> GetEffectivePermissions()
    {
        return RolePermissions
            .Where(rp => rp.IsActive)
            .Select(rp => rp.Permission)
            .ToHashSet();
    }

    // Factory methods
    public static Role CreateSystemRole(string name, string description)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            DisplayName = name,
            Description = description,
            IsSystemRole = true,
            IsActive = true
        };
    }

    public static Role CreateOrganizationalRole(string name, string description)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            DisplayName = name,
            Description = description,
            IsSystemRole = false,
            IsActive = true
        };
    }
}
