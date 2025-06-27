using System.Text.Json;
using System.Collections.Generic;
using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class Role : AuditableEntity
{
    // Identificación básica
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }

    // Jerarquía y herencia
    public Guid? ParentRoleId { get; set; }
    public bool IsInherited { get; set; }
    public int Level { get; set; }

    // Configuración del rol
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int? MaxUsers { get; set; }

    // Temporal y aprobación
    public bool IsTemporary { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApprovalWorkflow { get; set; }

    // Metadatos organizacionales
    public Guid? OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public int Priority { get; set; }

    // Navegación y relaciones
    public Role? ParentRole { get; set; }
    public ICollection<Role> ChildRoles { get; set; } = new List<Role>();
    
    // Relaciones many-to-many a través de entidades de unión
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Gestión de jerarquía
    public void AddChildRole(Role childRole)
    {
        if (childRole == null)
            throw new ArgumentNullException(nameof(childRole));

        if (ValidateHierarchy(childRole))
        {
            ChildRoles.Add(childRole);
            childRole.ParentRoleId = Id;
            childRole.Level = Level + 1;
        }
    }

    public void RemoveChildRole(Role childRole)
    {
        if (childRole == null)
            throw new ArgumentNullException(nameof(childRole));

        ChildRoles.Remove(childRole);
        childRole.ParentRoleId = null;
        childRole.Level = 0;
    }

    public IEnumerable<Role> GetAllAncestors()
    {
        var ancestors = new List<Role>();
        var currentRole = ParentRole;

        while (currentRole != null)
        {
            ancestors.Add(currentRole);
            currentRole = currentRole.ParentRole;
        }

        return ancestors;
    }

    public IEnumerable<Role> GetAllDescendants()
    {
        var descendants = new List<Role>();
        foreach (var childRole in ChildRoles)
        {
            descendants.Add(childRole);
            descendants.AddRange(childRole.GetAllDescendants());
        }

        return descendants;
    }

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
        var effectivePermissions = RolePermissions
            .Where(rp => rp.IsActive)
            .Select(rp => rp.Permission)
            .ToHashSet();

        if (IsInherited && ParentRole != null)
        {
            foreach (var parentPermission in ParentRole.GetEffectivePermissions())
            {
                effectivePermissions.Add(parentPermission);
            }
        }

        return effectivePermissions;
    }

    // Validaciones
    public bool CanBeAssignedTo(User user)
    {
        if (!IsActive || IsExpired())
            return false;

        if (MaxUsers.HasValue)
        {
            var activeUserCount = UserRoles.Count(ur => ur.IsActive);
            return activeUserCount < MaxUsers.Value;
        }

        return true;
    }

    public bool IsExpired()
    {
        return IsTemporary && ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    public bool IsWithinUserLimit()
    {
        if (!MaxUsers.HasValue) return true;
        
        var activeUserCount = UserRoles.Count(ur => ur.IsActive);
        return activeUserCount < MaxUsers.Value;
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
            IsActive = true,
            Level = 0,
            Priority = 1
        };
    }

    public static Role CreateOrganizationalRole(string name, string description, Guid organizationId)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            DisplayName = name,
            Description = description,
            OrganizationId = organizationId,
            IsSystemRole = false,
            IsActive = true,
            Level = 0,
            Priority = 5
        };
    }

    public static Role CreateTemporaryRole(string name, DateTime expiresAt)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            DisplayName = name,
            Description = $"Rol temporal que expira el {expiresAt.ToString("dd/MM/yyyy HH:mm")}",
            IsTemporary = true,
            ExpiresAt = expiresAt,
            IsActive = true,
            Level = 0,
            Priority = 3
        };
    }

    // Utilidades
    public int CalculateEffectiveLevel()
    {
        int effectiveLevel = Level;
        var currentRole = ParentRole;

        while (currentRole != null)
        {
            effectiveLevel += currentRole.Level;
            currentRole = currentRole.ParentRole;
        }

        return effectiveLevel;
    }

    private bool ValidateHierarchy(Role childRole)
    {
        var currentRole = this;
        while (currentRole != null)
        {
            if (currentRole.Id == childRole.Id)
                throw new InvalidOperationException("Circular role hierarchy detected.");

            currentRole = currentRole.ParentRole;
        }

        return true;
    }
}
