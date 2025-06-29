namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class RoleTests
{
    #region Helpers para pruebas
    private static Role CreateValidRole(string name = "TestRole")
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            DisplayName = name,
            Description = "Test role description",
            IsSystemRole = false,
            IsActive = true,
            Level = 0,
            Priority = 5,
            IsInherited = false,
            IsDefault = false,
            IsTemporary = false,
            RequiresApproval = false
        };
    }

    private static Permission CreateValidPermission(string name = "TestPermission")
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = name,
            Description = "Test permission",
            Category = "Test",
            Resource = ResourceType.Users,
            Action = ActionType.Read,
            Scope = PermissionScope.Global,
            IsSystemPermission = false,
            IsActive = true,
            RequiresApproval = false,
            RiskLevel = 1
        };
    }

    private static User CreateValidUser()
    {
        return new User(
            new Email("test@example.com"),
            "hashedpassword",
            "Test",
            "User")
        {
            Id = Guid.NewGuid(),
            Status = UserStatus.Active
        };
    }

    private static RolePermission CreateValidRolePermission(Guid roleId, Guid permissionId, Guid grantedBy)
    {
        return new RolePermission(roleId, permissionId, grantedBy);
    }
    #endregion

    #region Pruebas de métodos de fábrica

    [Fact]
    public void CreateSystemRole_DeberiaCrearRolDeSistemaValido()
    {
        // Arrange
        var name = "Administrator";
        var description = "System administrator role";

        // Act
        var role = Role.CreateSystemRole(name, description);

        // Assert
        role.Should().NotBeNull("El rol no debería ser nulo.");
        role.Name.Should().Be(name, "El nombre del rol debe coincidir.");
        role.NormalizedName.Should().Be(name.ToUpperInvariant(), "El nombre normalizado debe estar en mayúsculas.");
        role.DisplayName.Should().Be(name, "El nombre a mostrar debe coincidir.");
        role.Description.Should().Be(description, "La descripción del rol debe coincidir.");
        role.IsSystemRole.Should().BeTrue("Debe ser un rol de sistema.");
        role.IsActive.Should().BeTrue("El rol debe estar activo.");
        role.Level.Should().Be(0, "El nivel debe ser 0 para roles de sistema.");
        role.Priority.Should().Be(1, "La prioridad debe ser 1 para roles de sistema.");
    }

    [Fact]
    public void CreateOrganizationalRole_DeberiaCrearRolOrganizacionalValido()
    {
        // Arrange
        var name = "Manager";
        var description = "Department manager role";
        var organizationId = Guid.NewGuid();

        // Act
        var role = Role.CreateOrganizationalRole(name, description, organizationId);

        // Assert
        role.Should().NotBeNull("El rol organizacional no debería ser nulo.");
        role.Name.Should().Be(name, "El nombre del rol organizacional debe coincidir.");
        role.NormalizedName.Should().Be(name.ToUpperInvariant(), "El nombre normalizado debe estar en mayúsculas.");
        role.DisplayName.Should().Be(name, "El nombre a mostrar debe coincidir.");
        role.Description.Should().Be(description, "La descripción del rol organizacional debe coincidir.");
        role.OrganizationId.Should().Be(organizationId, "El ID de la organización debe coincidir.");
        role.IsSystemRole.Should().BeFalse("No debe ser un rol de sistema.");
        role.IsActive.Should().BeTrue("El rol debe estar activo.");
        role.Level.Should().Be(0, "El nivel debe ser 0 para roles organizacionales.");
        role.Priority.Should().Be(5, "La prioridad debe ser 5 para roles organizacionales.");
    }

    [Fact]
    public void CreateTemporaryRole_DeberiaCrearRolTemporalValido()
    {
        // Arrange
        var name = "ProjectLead";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var role = Role.CreateTemporaryRole(name, expiresAt);

        // Assert
        role.Should().NotBeNull("El rol temporal no debería ser nulo.");
        role.Name.Should().Be(name, "El nombre del rol temporal debe coincidir.");
        role.NormalizedName.Should().Be(name.ToUpperInvariant(), "El nombre normalizado debe estar en mayúsculas.");
        role.DisplayName.Should().Be(name, "El nombre a mostrar debe coincidir.");
        role.Description.Should().Contain("temporal", "La descripción debe contener la palabra 'temporal'.");
        role.IsTemporary.Should().BeTrue("Debe ser un rol temporal.");
        role.ExpiresAt.Should().Be(expiresAt, "La fecha de expiración debe coincidir.");
        role.IsActive.Should().BeTrue("El rol debe estar activo.");
        role.Level.Should().Be(0, "El nivel debe ser 0 para roles temporales.");
        role.Priority.Should().Be(3, "La prioridad debe ser 3 para roles temporales.");
    }
    #endregion

    #region Pruebas de gestión jerárquica

    [Fact]
    public void AddChildRole_CuandoHijoEsValido_DeberiaAñadirExitosamente()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");
        parentRole.Level = 1;
        var childRole = CreateValidRole("Child");

        // Act
        parentRole.AddChildRole(childRole);

        // Assert
        parentRole.ChildRoles.Should().Contain(childRole, "El rol padre debe contener el rol hijo.");
        childRole.ParentRoleId.Should().Be(parentRole.Id, "El rol hijo debe tener el ParentRoleId del rol padre.");
        childRole.Level.Should().Be(parentRole.Level + 1, "El nivel del rol hijo debe ser uno más que el del padre.");
    }

    [Fact]
    public void AddChildRole_CuandoHijoEsNulo_DeberiaLanzarArgumentNullException()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");

        // Act & Assert
        var act = () => parentRole.AddChildRole(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("childRole");
    }

    [Fact]
    public void AddChildRole_CuandoJerarquiaEsCircular_DeberiaLanzarInvalidOperationException()
    {
        // Arrange
        var role1 = CreateValidRole("Role1");
        var role2 = CreateValidRole("Role2");
        
        // Crear una jerarquía: role1 -> role2
        role1.AddChildRole(role2);
        role2.ParentRole = role1; // EF establecería esto automáticamente

        // Act & Assert - Intentar crear una jerarquía circular: role2 -> role1
        var act = () => role2.AddChildRole(role1);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Circular role hierarchy detected*", "Debe detectar y lanzar excepción por jerarquía circular.");
    }

    [Fact]
    public void RemoveChildRole_CuandoHijoEsValido_DeberiaRemoverExitosamente()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");
        var childRole = CreateValidRole("Child");
        parentRole.AddChildRole(childRole);

        // Act
        parentRole.RemoveChildRole(childRole);

        // Assert
        parentRole.ChildRoles.Should().NotContain(childRole, "El rol padre no debe contener el rol hijo después de la eliminación.");
        childRole.ParentRoleId.Should().BeNull("El ParentRoleId del rol hijo debe ser nulo después de la eliminación.");
        childRole.Level.Should().Be(0, "El nivel del rol hijo debe restablecerse a 0.");
    }

    [Fact]
    public void RemoveChildRole_CuandoHijoEsNulo_DeberiaLanzarArgumentNullException()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");

        // Act & Assert
        var act = () => parentRole.RemoveChildRole(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("childRole");
    }

    [Fact]
    public void GetAllAncestors_CuandoTienePadres_DeberiaRetornarTodosLosAncestros()
    {
        // Arrange
        var grandParent = CreateValidRole("GrandParent");
        var parent = CreateValidRole("Parent");
        var child = CreateValidRole("Child");

        // Simular la configuración de la propiedad de navegación de Entity Framework
        grandParent.AddChildRole(parent);
        parent.ParentRole = grandParent; // EF establecería esto automáticamente
        
        parent.AddChildRole(child);
        child.ParentRole = parent; // EF establecería esto automáticamente

        // Act
        var ancestors = child.GetAllAncestors().ToList();

        // Assert
        ancestors.Should().HaveCount(2, "Debe devolver 2 ancestros.");
        ancestors.Should().Contain(parent, "Debe contener al padre.");
        ancestors.Should().Contain(grandParent, "Debe contener al abuelo.");
    }

    [Fact]
    public void GetAllAncestors_CuandoNoTienePadres_DeberiaRetornarVacio()
    {
        // Arrange
        var role = CreateValidRole("Orphan");

        // Act
        var ancestors = role.GetAllAncestors();

        // Assert
        ancestors.Should().BeEmpty("Debe devolver una lista vacía si no hay ancestros.");
    }

    [Fact]
    public void GetAllDescendants_CuandoTieneHijos_DeberiaRetornarTodosLosDescendientes()
    {
        // Arrange
        var grandParent = CreateValidRole("GrandParent");
        var parent = CreateValidRole("Parent");
        var child = CreateValidRole("Child");

        // Simular la configuración de la propiedad de navegación de Entity Framework
        grandParent.AddChildRole(parent);
        parent.ParentRole = grandParent; // EF establecería esto automáticamente
        
        parent.AddChildRole(child);
        child.ParentRole = parent; // EF establecería esto automáticamente

        // Act
        var descendants = grandParent.GetAllDescendants().ToList();

        // Assert
        descendants.Should().HaveCount(2, "Debe devolver 2 descendientes.");
        descendants.Should().Contain(parent, "Debe contener al hijo directo.");
        descendants.Should().Contain(child, "Debe contener al nieto.");
    }

    [Fact]
    public void GetAllDescendants_CuandoNoTieneHijos_DeberiaRetornarVacio()
    {
        // Arrange
        var role = CreateValidRole("Childless");

        // Act
        var descendants = role.GetAllDescendants();

        // Assert
        descendants.Should().BeEmpty("Debe devolver una lista vacía si no hay descendientes.");
    }
    #endregion

    #region Pruebas de gestión de permisos

    [Fact]
    public void AddPermission_CuandoPermisoEsValido_DeberiaAñadirExitosamente()
    {
        // Arrange
        var role = CreateValidRole();
        var permission = CreateValidPermission();
        var grantedBy = Guid.NewGuid();

        // Act
        role.AddPermission(permission, grantedBy);

        // Assert
        role.RolePermissions.Should().HaveCount(1, "Debe haber una RolePermission después de añadir.");
        var rolePermission = role.RolePermissions.First();
        rolePermission.PermissionId.Should().Be(permission.Id, "El PermissionId debe coincidir.");
        rolePermission.GrantedBy.Should().Be(grantedBy, "El GrantedBy debe coincidir.");
        rolePermission.IsActive.Should().BeTrue("La RolePermission debe estar activa.");
    }

    [Fact]
    public void AddPermission_CuandoPermisoEsNulo_DeberiaLanzarArgumentNullException()
    {
        // Arrange
        var role = CreateValidRole();
        var grantedBy = Guid.NewGuid();

        // Act & Assert
        var act = () => role.AddPermission(null!, grantedBy);
        act.Should().Throw<ArgumentNullException>().WithParameterName("permission");
    }

    [Fact]
    public void AddPermission_CuandoPermisoYaExiste_NoDeberiaDuplicar()
    {
        // Arrange
        var role = CreateValidRole();
        var permission = CreateValidPermission();
        var grantedBy = Guid.NewGuid();
        
        var existingRolePermission = CreateValidRolePermission(role.Id, permission.Id, grantedBy);
        role.RolePermissions.Add(existingRolePermission);

        // Act
        role.AddPermission(permission, grantedBy);

        // Assert
        role.RolePermissions.Should().HaveCount(1, "No debe haber duplicados si el permiso ya existe.");
    }

    [Fact]
    public void RemovePermission_CuandoPermisoExiste_DeberiaRevocarPermiso()
    {
        // Arrange
        var role = CreateValidRole();
        var permission = CreateValidPermission();
        var grantedBy = Guid.NewGuid();
        
        var rolePermission = CreateValidRolePermission(role.Id, permission.Id, grantedBy);
        role.RolePermissions.Add(rolePermission);

        // Act
        role.RemovePermission(permission.Id);

        // Assert
        rolePermission.IsActive.Should().BeFalse("La RolePermission debe estar inactiva después de ser revocada.");
    }

    [Fact]
    public void RemovePermission_CuandoPermisoNoExiste_NoDeberiaLanzarExcepcion()
    {
        // Arrange
        var role = CreateValidRole();
        var nonExistentPermissionId = Guid.NewGuid();

        // Act & Assert
        var act = () => role.RemovePermission(nonExistentPermissionId);
        act.Should().NotThrow("No debe lanzar excepción si el permiso a remover no existe.");
    }

    [Fact]
    public void HasPermission_CuandoPermisoExiste_DeberiaRetornarTrue()
    {
        // Arrange
        var role = CreateValidRole();
        var permission = CreateValidPermission("ReadUsers");
        var grantedBy = Guid.NewGuid();
        
        var rolePermission = CreateValidRolePermission(role.Id, permission.Id, grantedBy);
        rolePermission.Permission = permission;
        role.RolePermissions.Add(rolePermission);

        // Act
        var hasPermission = role.HasPermission("ReadUsers");

        // Assert
        hasPermission.Should().BeTrue("Debe tener el permiso si existe y está activo.");
    }

    [Fact]
    public void HasPermission_CuandoPermisoNoExiste_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();

        // Act
        var hasPermission = role.HasPermission("NonExistentPermission");

        // Assert
        hasPermission.Should().BeFalse("No debe tener el permiso si no existe.");
    }

    [Fact]
    public void HasPermission_CuandoPermisoEstaInactivo_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        var permission = CreateValidPermission("ReadUsers");
        var grantedBy = Guid.NewGuid();
        
        var rolePermission = CreateValidRolePermission(role.Id, permission.Id, grantedBy);
        rolePermission.Permission = permission;
        rolePermission.Revoke(); // Hacer inactivo
        role.RolePermissions.Add(rolePermission);

        // Act
        var hasPermission = role.HasPermission("ReadUsers");

        // Assert
        hasPermission.Should().BeFalse("No debe tener el permiso si está inactivo.");
    }

    [Fact]
    public void GetEffectivePermissions_CuandoRolTienePermisosDirectos_DeberiaRetornarPermisosDirectos()
    {
        // Arrange
        var role = CreateValidRole();
        var permission1 = CreateValidPermission("Permission1");
        var permission2 = CreateValidPermission("Permission2");
        var grantedBy = Guid.NewGuid();
        
        var rolePermission1 = CreateValidRolePermission(role.Id, permission1.Id, grantedBy);
        rolePermission1.Permission = permission1;
        var rolePermission2 = CreateValidRolePermission(role.Id, permission2.Id, grantedBy);
        rolePermission2.Permission = permission2;
        
        role.RolePermissions.Add(rolePermission1);
        role.RolePermissions.Add(rolePermission2);

        // Act
        var effectivePermissions = role.GetEffectivePermissions().ToList();

        // Assert
        effectivePermissions.Should().HaveCount(2, "Debe devolver 2 permisos efectivos.");
        effectivePermissions.Should().Contain(permission1, "Debe contener el permiso 1.");
        effectivePermissions.Should().Contain(permission2, "Debe contener el permiso 2.");
    }

    [Fact]
    public void GetEffectivePermissions_CuandoRolHeredaDePadre_DeberiaIncluirPermisosDelPadre()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");
        var childRole = CreateValidRole("Child");
        childRole.IsInherited = true;
        childRole.ParentRole = parentRole; // Establecer la propiedad de navegación

        var parentPermission = CreateValidPermission("ParentPermission");
        var childPermission = CreateValidPermission("ChildPermission");
        var grantedBy = Guid.NewGuid();

        var parentRolePermission = CreateValidRolePermission(parentRole.Id, parentPermission.Id, grantedBy);
        parentRolePermission.Permission = parentPermission;
        parentRole.RolePermissions.Add(parentRolePermission);

        var childRolePermission = CreateValidRolePermission(childRole.Id, childPermission.Id, grantedBy);
        childRolePermission.Permission = childPermission;
        childRole.RolePermissions.Add(childRolePermission);

        // Act
        var effectivePermissions = childRole.GetEffectivePermissions().ToList();

        // Assert
        effectivePermissions.Should().HaveCount(2, "Debe devolver 2 permisos efectivos (directos e heredados).");
        effectivePermissions.Should().Contain(parentPermission, "Debe contener el permiso del padre.");
        effectivePermissions.Should().Contain(childPermission, "Debe contener el permiso del hijo.");
    }
    #endregion

    #region Pruebas de validación

    [Fact]
    public void CanBeAssignedTo_CuandoRolEstaActivoYNoExpirado_DeberiaRetornarTrue()
    {
        // Arrange
        var role = CreateValidRole();
        var user = CreateValidUser();

        // Act
        var canBeAssigned = role.CanBeAssignedTo(user);

        // Assert
        canBeAssigned.Should().BeTrue("El rol debe poder asignarse si está activo y no ha expirado.");
    }

    [Fact]
    public void CanBeAssignedTo_CuandoRolEstaInactivo_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.IsActive = false;
        var user = CreateValidUser();

        // Act
        var canBeAssigned = role.CanBeAssignedTo(user);

        // Assert
        canBeAssigned.Should().BeFalse("El rol no debe poder asignarse si está inactivo.");
    }

    [Fact]
    public void CanBeAssignedTo_CuandoRolHaExpirado_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.IsTemporary = true;
        role.ExpiresAt = DateTime.UtcNow.AddDays(-1); // Expirado ayer
        var user = CreateValidUser();

        // Act
        var canBeAssigned = role.CanBeAssignedTo(user);

        // Assert
        canBeAssigned.Should().BeFalse("El rol no debe poder asignarse si ha expirado.");
    }

    [Fact]
    public void CanBeAssignedTo_CuandoSeAlcanzaMaximoDeUsuarios_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.MaxUsers = 1;
        var user = CreateValidUser();

        // Añadir un rol de usuario activo para alcanzar el límite
        var activeUserRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = role.Id,
            IsActive = true,
            User = CreateValidUser(),
            Role = role,
            AssignedByUser = CreateValidUser()
        };
        role.UserRoles.Add(activeUserRole);

        // Act
        var canBeAssigned = role.CanBeAssignedTo(user);

        // Assert
        canBeAssigned.Should().BeFalse("El rol no debe poder asignarse si se ha alcanzado el límite de usuarios.");
    }

    [Fact]
    public void IsExpired_CuandoEsTemporalYExpiracionPaso_DeberiaRetornarTrue()
    {
        // Arrange
        var role = CreateValidRole();
        role.IsTemporary = true;
        role.ExpiresAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var isExpired = role.IsExpired();

        // Assert
        isExpired.Should().BeTrue("El rol temporal debe estar expirado si su fecha de expiración ha pasado.");
    }

    [Fact]
    public void IsExpired_CuandoNoEsTemporal_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.IsTemporary = false;

        // Act
        var isExpired = role.IsExpired();

        // Assert
        isExpired.Should().BeFalse("El rol no temporal no debe estar expirado.");
    }

    [Fact]
    public void IsExpired_CuandoEsTemporalPeroNoHaExpirado_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.IsTemporary = true;
        role.ExpiresAt = DateTime.UtcNow.AddDays(1);

        // Act
        var isExpired = role.IsExpired();

        // Assert
        isExpired.Should().BeFalse("El rol temporal no debe estar expirado si su fecha de expiración no ha pasado.");
    }

    [Fact]
    public void IsWithinUserLimit_CuandoNoHayMaxUsers_DeberiaRetornarTrue()
    {
        // Arrange
        var role = CreateValidRole();
        role.MaxUsers = null;

        // Act
        var isWithinLimit = role.IsWithinUserLimit();

        // Assert
        isWithinLimit.Should().BeTrue("Debe estar dentro del límite si no hay MaxUsers definido.");
    }

    [Fact]
    public void IsWithinUserLimit_CuandoEstaDebajoDeMaxUsers_DeberiaRetornarTrue()
    {
        // Arrange
        var role = CreateValidRole();
        role.MaxUsers = 2;

        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = role.Id,
            IsActive = true,
            User = CreateValidUser(),
            Role = role,
            AssignedByUser = CreateValidUser()
        };
        role.UserRoles.Add(userRole);

        // Act
        var isWithinLimit = role.IsWithinUserLimit();

        // Assert
        isWithinLimit.Should().BeTrue("Debe estar dentro del límite si los usuarios activos son menos que MaxUsers.");
    }

    [Fact]
    public void IsWithinUserLimit_CuandoEstaEnMaxUsers_DeberiaRetornarFalse()
    {
        // Arrange
        var role = CreateValidRole();
        role.MaxUsers = 1;

        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = role.Id,
            IsActive = true,
            User = CreateValidUser(),
            Role = role,
            AssignedByUser = CreateValidUser()
        };
        role.UserRoles.Add(userRole);

        // Act
        var isWithinLimit = role.IsWithinUserLimit();

        // Assert
        isWithinLimit.Should().BeFalse("No debe estar dentro del límite si los usuarios activos son iguales a MaxUsers.");
    }
    #endregion

    #region Pruebas de métodos utilitarios

    [Fact]
    public void CalculateEffectiveLevel_CuandoNoHayPadre_DeberiaRetornarPropioNivel()
    {
        // Arrange
        var role = CreateValidRole();
        role.Level = 3;

        // Act
        var effectiveLevel = role.CalculateEffectiveLevel();

        // Assert
        effectiveLevel.Should().Be(3, "El nivel efectivo debe ser igual al propio nivel si no hay padre.");
    }

    [Fact]
    public void CalculateEffectiveLevel_CuandoTienePadres_DeberiaSumarTodosLosNiveles()
    {
        // Arrange
        var grandParent = CreateValidRole("GrandParent");
        grandParent.Level = 1;
        
        var parent = CreateValidRole("Parent");
        parent.Level = 2;
        parent.ParentRole = grandParent;
        grandParent.ParentRole = null; // Asegura que no haya más padres
        
        var child = CreateValidRole("Child");
        child.Level = 3;
        child.ParentRole = parent;

        // Act
        var effectiveLevel = child.CalculateEffectiveLevel();

        // Assert
        effectiveLevel.Should().Be(6, "El nivel efectivo debe ser la suma de los niveles de todos los ancestros más el propio."); // 3 + 2 + 1
    }

    [Fact]
    public void ValidateHierarchy_CuandoJerarquiaEsValida_NoDeberiaLanzarExcepcion()
    {
        // Arrange
        var parentRole = CreateValidRole("Parent");
        var childRole = CreateValidRole("Child");

        // Act & Assert
        var act = () => parentRole.AddChildRole(childRole);
        act.Should().NotThrow("Una jerarquía válida no debe lanzar excepciones.");
    }

    [Fact]
    public void ValidateHierarchy_CuandoHayAutoreferencia_DeberiaLanzarInvalidOperationException()
    {
        // Arrange
        var role = CreateValidRole();

        // Act & Assert
        var act = () => role.AddChildRole(role);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Circular role hierarchy detected*", "Debe lanzar excepción por referencia circular.");
    }
    #endregion
}