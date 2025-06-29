using System.Text.Json;

namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class PermissionTests
{
    #region Helpers para pruebas
    private static Permission CreateValidPermission(
        string name = "TestPermission",
        ResourceType resource = ResourceType.Users,
        ActionType action = ActionType.Read,
        PermissionScope scope = PermissionScope.Global)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = name,
            Description = "Test permission description",
            Category = "Test",
            Resource = resource,
            Action = action,
            Scope = scope,
            IsSystemPermission = false,
            IsActive = true,
            RequiresApproval = false,
            RiskLevel = 5
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
    public void CreateSystemPermission_DeberiaCrearPermisoDeSistemaValido()
    {
        // Arrange
        var name = "Users.Read";
        var resource = ResourceType.Users;
        var action = ActionType.Read;

        // Act
        var permission = Permission.CreateSystemPermission(name, resource, action);

        // Assert
        permission.Should().NotBeNull("El objeto de permiso no debe ser nulo.");
        permission.Name.Should().Be(name, "El nombre del permiso debe coincidir.");
        permission.DisplayName.Should().Be(name, "El nombre a mostrar debe coincidir.");
        permission.Description.Should().Contain("sistema", "La descripción debe contener 'sistema'.");
        permission.Description.Should().Contain("Users.Read", "La descripción debe contener el nombre completo del permiso.");
        permission.Category.Should().Be("Sistema", "La categoría debe ser 'Sistema'.");
        permission.Resource.Should().Be(resource, "El recurso debe coincidir.");
        permission.Action.Should().Be(action, "La acción debe coincidir.");
        permission.Scope.Should().Be(PermissionScope.Global, "El alcance debe ser Global.");
        permission.IsSystemPermission.Should().BeTrue("Debe ser un permiso de sistema.");
        permission.IsActive.Should().BeTrue("El permiso debe estar activo.");
        permission.RiskLevel.Should().Be(1, "El nivel de riesgo debe ser 1 para permisos de sistema.");
    }

    [Fact]
    public void CreateCustomPermission_DeberiaCrearPermisoPersonalizadoValido()
    {
        // Arrange
        var name = "CustomPermission";
        var description = "Custom permission for testing";
        var resource = ResourceType.Settings;
        var action = ActionType.Update;
        var scope = PermissionScope.Organization;

        // Act
        var permission = Permission.CreateCustomPermission(name, description, resource, action, scope);

        // Assert
        permission.Should().NotBeNull("El objeto de permiso personalizado no debe ser nulo.");
        permission.Name.Should().Be(name, "El nombre del permiso personalizado debe coincidir.");
        permission.DisplayName.Should().Be(name, "El nombre a mostrar debe coincidir.");
        permission.Description.Should().Be(description, "La descripción debe coincidir.");
        permission.Category.Should().Be("Custom", "La categoría debe ser 'Custom'.");
        permission.Resource.Should().Be(resource, "El recurso debe coincidir.");
        permission.Action.Should().Be(action, "La acción debe coincidir.");
        permission.Scope.Should().Be(scope, "El alcance debe coincidir.");
        permission.IsSystemPermission.Should().BeFalse("No debe ser un permiso de sistema.");
        permission.IsActive.Should().BeTrue("El permiso debe estar activo.");
        permission.RequiresApproval.Should().BeFalse("No debe requerir aprobación.");
        permission.RiskLevel.Should().Be(5, "El nivel de riesgo debe ser 5 para permisos personalizados.");
    }

    [Theory]
    [InlineData(ResourceType.Users, ActionType.Create)]
    [InlineData(ResourceType.Roles, ActionType.Delete)]
    [InlineData(ResourceType.Permissions, ActionType.Update)]
    [InlineData(ResourceType.Sessions, ActionType.List)]
    public void CreateSystemPermission_ConDiferentesRecursosYAcciones_DeberiaCrearCorrectamente(
        ResourceType resource, ActionType action)
    {
        // Arrange
        var name = $"{resource}.{action}";

        // Act
        var permission = Permission.CreateSystemPermission(name, resource, action);

        // Assert
        permission.Resource.Should().Be(resource, "El recurso de la permisión debe coincidir.");
        permission.Action.Should().Be(action, "La acción de la permisión debe coincidir.");
        permission.Name.Should().Be(name, "El nombre de la permisión debe coincidir.");
        permission.IsSystemPermission.Should().BeTrue("La permisión debe ser de sistema.");
    }

    #endregion

    #region Pruebas de validación

    [Fact]
    public void IsValidForUser_CuandoPermisoEstaActivoYNoExpirado_DeberiaRetornarTrue()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = true;
        permission.ValidFrom = null;
        permission.ValidUntil = null;
        var user = CreateValidUser();

        // Act
        var isValid = permission.IsValidForUser(user);

        // Assert
        isValid.Should().BeTrue("El permiso debe ser válido cuando está activo y no ha expirado.");
    }

    [Fact]
    public void IsValidForUser_CuandoPermisoEstaInactivo_DeberiaRetornarFalse()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = false;
        var user = CreateValidUser();

        // Act
        var isValid = permission.IsValidForUser(user);

        // Assert
        isValid.Should().BeFalse("El permiso no debe ser válido cuando está inactivo.");
    }

    [Fact]
    public void IsValidForUser_CuandoPermisoHaExpirado_DeberiaRetornarFalse()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = true;
        permission.ValidUntil = DateTime.UtcNow.AddDays(-1); // Expired yesterday
        var user = CreateValidUser();

        // Act
        var isValid = permission.IsValidForUser(user);

        // Assert
        isValid.Should().BeFalse("El permiso no debe ser válido cuando ha expirado.");
    }

    [Fact]
    public void IsValidForUser_CuandoPermisoTieneCondicionesJsonValidas_DeberiaRetornarTrue()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = true;
        permission.Conditions = JsonSerializer.Serialize(new { department = "IT", level = 5 });
        var user = CreateValidUser();

        // Act
        var isValid = permission.IsValidForUser(user);

        // Assert
        isValid.Should().BeTrue("El permiso debe ser válido cuando las condiciones JSON son válidas.");
    }

    [Fact]
    public void IsValidForUser_CuandoPermisoTieneCondicionesJsonInvalidas_DeberiaRetornarFalse()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = true;
        permission.Conditions = "{ invalid json }";
        var user = CreateValidUser();

        // Act
        var isValid = permission.IsValidForUser(user);

        // Assert
        isValid.Should().BeFalse("El permiso no debe ser válido cuando las condiciones JSON son inválidas.");
    }

    [Fact]
    public void IsExpired_CuandoValidFromEsEnFuturo_DeberiaRetornarTrue()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.ValidFrom = DateTime.UtcNow.AddDays(1); // Inicia mañana

        // Act
        var isExpired = permission.IsExpired();

        // Assert
        isExpired.Should().BeTrue("El permiso debe considerarse expirado si su fecha de inicio es en el futuro.");
    }

    [Fact]
    public void IsExpired_CuandoValidUntilEsEnPasado_DeberiaRetornarTrue()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.ValidUntil = DateTime.UtcNow.AddDays(-1); // Terminó ayer

        // Act
        var isExpired = permission.IsExpired();

        // Assert
        isExpired.Should().BeTrue("El permiso debe considerarse expirado si su fecha de fin es en el pasado.");
    }

    [Fact]
    public void IsExpired_CuandoEstaDentroDePeriodoValido_DeberiaRetornarFalse()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.ValidFrom = DateTime.UtcNow.AddDays(-1); // Inició ayer
        permission.ValidUntil = DateTime.UtcNow.AddDays(1); // Termina mañana

        // Act
        var isExpired = permission.IsExpired();

        // Assert
        isExpired.Should().BeFalse("El permiso no debe considerarse expirado si está dentro del período de validez.");
    }

    [Fact]
    public void IsExpired_CuandoNoHayPeriodoDeValidez_DeberiaRetornarFalse()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.ValidFrom = null;
        permission.ValidUntil = null;

        // Act
        var isExpired = permission.IsExpired();

        // Assert
        isExpired.Should().BeFalse("El permiso no debe considerarse expirado si no tiene período de validez establecido.");
    }

    [Theory]
    [InlineData("Users", "Read", ResourceType.Users, ActionType.Read, true)]
    [InlineData("users", "read", ResourceType.Users, ActionType.Read, true)] // Case insensitive (minúsculas)
    [InlineData("USERS", "READ", ResourceType.Users, ActionType.Read, true)] // Case insensitive (mayúsculas)
    [InlineData("Users", "Write", ResourceType.Users, ActionType.Read, false)] // Diferente acción
    [InlineData("Roles", "Read", ResourceType.Users, ActionType.Read, false)] // Diferente recurso
    public void MatchesResource_DeberiaCoincidirRecursoYAccionCorrectamente(
        string resourceString, string actionString, ResourceType permissionResource, ActionType permissionAction, bool expectedResult)
    {
        // Arrange
        var permission = CreateValidPermission(resource: permissionResource, action: permissionAction);

        // Act
        var matches = permission.MatchesResource(resourceString, actionString);

        // Assert
        matches.Should().Be(expectedResult, $"La coincidencia de recurso y acción debería ser {expectedResult}.");
    }

    #endregion

    #region Pruebas de métodos utilitarios

    [Fact]
    public void GetFullPermissionString_DeberiaRetornarFormatoCorrecto()
    {
        // Arrange
        var permission = CreateValidPermission(
            resource: ResourceType.Users,
            action: ActionType.Create,
            scope: PermissionScope.Organization);

        // Act
        var fullString = permission.GetFullPermissionString();

        // Assert
        fullString.Should().Be("Users:Create:Organization", "La cadena de permiso completa debe tener el formato esperado.");
    }

    [Theory]
    [InlineData(ResourceType.Users, ActionType.Read, PermissionScope.Global, "Users:Read:Global")]
    [InlineData(ResourceType.Roles, ActionType.Delete, PermissionScope.Department, "Roles:Delete:Department")]
    [InlineData(ResourceType.Settings, ActionType.Update, PermissionScope.Own, "Settings:Update:Own")]
    public void GetFullPermissionString_ConDiferentesValores_DeberiaRetornarFormatoCorrecto(
        ResourceType resource, ActionType action, PermissionScope scope, string expected)
    {
        // Arrange
        var permission = CreateValidPermission(resource: resource, action: action, scope: scope);

        // Act
        var fullString = permission.GetFullPermissionString();

        // Assert
        fullString.Should().Be(expected, "La cadena de permiso completa debe coincidir con el valor esperado.");
    }

    [Fact]
    public void GetPermissionParts_DeberiaRetornarTuplaCorrecta()
    {
        // Arrange
        var permission = CreateValidPermission(
            resource: ResourceType.Audit,
            action: ActionType.Export,
            scope: PermissionScope.Global);

        // Act
        var (resource, action, scope) = permission.GetPermissionParts();

        // Assert
        resource.Should().Be(ResourceType.Audit, "El tipo de recurso debe ser Audit.");
        action.Should().Be(ActionType.Export, "El tipo de acción debe ser Export.");
        scope.Should().Be(PermissionScope.Global, "El alcance debe ser Global.");
    }

    #endregion

    #region Pruebas de propiedades individuales

    [Fact]
    public void Permission_ConPropiedadesRequeridas_DeberiaSerValido()
    {
        // Arrange & Act
        var permission = new Permission
        {
            Name = "TestPermission",
            DisplayName = "Test Permission",
            Description = "A test permission",
            Category = "Test",
            Resource = ResourceType.Users,
            Action = ActionType.Read,
            Scope = PermissionScope.Global,
            IsActive = true
        };

        // Assert
        permission.Name.Should().Be("TestPermission", "El nombre del permiso debe ser 'TestPermission'.");
        permission.DisplayName.Should().Be("Test Permission", "El nombre a mostrar debe ser 'Test Permission'.");
        permission.Description.Should().Be("A test permission", "La descripción debe ser 'A test permission'.");
        permission.Category.Should().Be("Test", "La categoría debe ser 'Test'.");
        permission.Resource.Should().Be(ResourceType.Users, "El recurso debe ser Users.");
        permission.Action.Should().Be(ActionType.Read, "La acción debe ser Read.");
        permission.Scope.Should().Be(PermissionScope.Global, "El alcance debe ser Global.");
        permission.IsActive.Should().BeTrue("El permiso debe estar activo.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Permission_ConDiferentesNivelesDeRiesgo_DeberiaAlmacenarCorrectamente(int riskLevel)
    {
        // Arrange
        var permission = CreateValidPermission();

        // Act
        permission.RiskLevel = riskLevel;

        // Assert
        permission.RiskLevel.Should().Be(riskLevel, "El nivel de riesgo debe coincidir con el valor establecido.");
    }

    [Fact]
    public void Permission_ConValidezTemporal_DeberiaManejarCorrectamente()
    {
        // Arrange
        var permission = CreateValidPermission();
        var validFrom = DateTime.UtcNow.AddDays(-1);
        var validUntil = DateTime.UtcNow.AddDays(30);

        // Act
        permission.ValidFrom = validFrom;
        permission.ValidUntil = validUntil;

        // Assert
        permission.ValidFrom.Should().Be(validFrom, "La fecha de inicio de validez debe coincidir.");
        permission.ValidUntil.Should().Be(validUntil, "La fecha de fin de validez debe coincidir.");
        permission.IsExpired().Should().BeFalse("El permiso no debe estar expirado.");
    }

    [Fact]
    public void Permission_ConCondicionesComplejas_DeberiaAlmacenarJsonCorrectamente()
    {
        // Arrange
        var permission = CreateValidPermission();
        var conditions = new
        {
            department = "IT",
            level = 5,
            timeRestriction = new { startHour = 9, endHour = 17 },
            allowedDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }
        };
        var conditionsJson = JsonSerializer.Serialize(conditions);

        // Act
        permission.Conditions = conditionsJson;

        // Assert
        permission.Conditions.Should().NotBeNullOrEmpty("Las condiciones no deben ser nulas ni vacías.");
        permission.Conditions.Should().Contain("department", "Las condiciones deben contener la clave 'department'.");
        permission.Conditions.Should().Contain("IT", "Las condiciones deben contener el valor 'IT'.");
        permission.Conditions.Should().Contain("timeRestriction", "Las condiciones deben contener la clave 'timeRestriction'.");

        // Verificar que se pueda deserializar de nuevo
        var deserializedConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(permission.Conditions!);
        deserializedConditions.Should().ContainKey("department", "Las condiciones deserializadas deben contener la clave 'department'.");
        deserializedConditions.Should().ContainKey("level", "Las condiciones deserializadas deben contener la clave 'level'.");
    }

    #endregion

    #region Pruebas de propiedades de navegación

    [Fact]
    public void Permission_DeberiaInicializarColeccionRolePermissions()
    {
        // Arrange & Act
        var permission = CreateValidPermission();

        // Assert
        permission.RolePermissions.Should().NotBeNull("La colección RolePermissions no debe ser nula.");
        permission.RolePermissions.Should().BeEmpty("La colección RolePermissions debe estar vacía al inicio.");
    }

    [Fact]
    public void Permission_DeberiaPermitirAgregarRolePermissions()
    {
        // Arrange
        var permission = CreateValidPermission();
        var roleId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();
        var rolePermission = CreateValidRolePermission(roleId, permission.Id, grantedBy);

        // Act
        permission.RolePermissions.Add(rolePermission);

        // Assert
        permission.RolePermissions.Should().HaveCount(1, "La colección RolePermissions debe contener un elemento.");
        permission.RolePermissions.Should().Contain(rolePermission, "La colección RolePermissions debe contener el elemento añadido.");
    }

    #endregion

    #region Pruebas de integración

    [Fact]
    public void CicloDeVidaDePermiso_DeberiaFuncionarCorrectamente()
    {
        // Arrange & Act - Crear permiso de sistema
        var systemPermission = Permission.CreateSystemPermission("Users.Delete", ResourceType.Users, ActionType.Delete);

        // Assert - Propiedades del permiso de sistema
        systemPermission.IsSystemPermission.Should().BeTrue("Debe ser un permiso de sistema.");
        systemPermission.RiskLevel.Should().Be(1, "El nivel de riesgo debe ser 1.");
        systemPermission.Scope.Should().Be(PermissionScope.Global, "El alcance debe ser Global.");

        // Act - Prueba de validación
        var user = CreateValidUser();
        systemPermission.IsValidForUser(user).Should().BeTrue("El permiso del sistema debe ser válido para el usuario.");

        // Act - Prueba de coincidencia de recursos
        systemPermission.MatchesResource("Users", "Delete").Should().BeTrue("Debe coincidir con el recurso Users y la acción Delete.");
        systemPermission.MatchesResource("Users", "Create").Should().BeFalse("No debe coincidir con la acción Create.");

        // Act - Prueba de métodos utilitarios
        systemPermission.GetFullPermissionString().Should().Be("Users:Delete:Global", "La cadena de permiso completa debe ser correcta.");
        var (resource, action, scope) = systemPermission.GetPermissionParts();
        resource.Should().Be(ResourceType.Users, "El recurso debe ser Users.");
        action.Should().Be(ActionType.Delete, "La acción debe ser Delete.");
        scope.Should().Be(PermissionScope.Global, "El alcance debe ser Global.");
    }

    [Fact]
    public void PermisoPersonalizadoConCondiciones_DeberiaFuncionarCorrectamente()
    {
        // Arrange & Act - Crear permiso personalizado
        var customPermission = Permission.CreateCustomPermission(
            "DepartmentRead",
            "Read access to department data",
            ResourceType.Users,
            ActionType.Read,
            PermissionScope.Department);

        // Act - Añadir condiciones
        var conditions = new { department = "HR", accessLevel = 3 };
        customPermission.Conditions = JsonSerializer.Serialize(conditions);

        // Act - Establecer validez temporal
        customPermission.ValidFrom = DateTime.UtcNow.AddDays(-1);
        customPermission.ValidUntil = DateTime.UtcNow.AddDays(30);

        // Assert - Propiedades del permiso personalizado
        customPermission.IsSystemPermission.Should().BeFalse("No debe ser un permiso de sistema.");
        customPermission.Scope.Should().Be(PermissionScope.Department, "El alcance debe ser Department.");
        customPermission.RiskLevel.Should().Be(5, "El nivel de riesgo debe ser 5.");

        // Assert - Validación
        var user = CreateValidUser();
        customPermission.IsValidForUser(user).Should().BeTrue("El permiso personalizado debe ser válido para el usuario.");
        customPermission.IsExpired().Should().BeFalse("El permiso no debe estar expirado.");

        // Assert - Condiciones son JSON válido
        var deserializedConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(customPermission.Conditions!);
        deserializedConditions.Should().ContainKey("department", "Las condiciones deserializadas deben contener la clave 'department'.");
    }

    [Fact]
    public void ComparacionDePermisos_DeberiaFuncionarConDiferentesScopes()
    {
        // Arrange
        var globalPermission = Permission.CreateSystemPermission("Users.Read", ResourceType.Users, ActionType.Read);
        var orgPermission = Permission.CreateCustomPermission(
            "Users.Read.Org", "Organization user read", ResourceType.Users, ActionType.Read, PermissionScope.Organization);
        var ownPermission = Permission.CreateCustomPermission(
            "Users.Read.Own", "Own user read", ResourceType.Users, ActionType.Read, PermissionScope.Own);

        // Act & Assert - Todos coinciden con el mismo recurso/acción pero diferentes alcances
        globalPermission.MatchesResource("Users", "Read").Should().BeTrue("El permiso global debe coincidir con el recurso y la acción.");
        orgPermission.MatchesResource("Users", "Read").Should().BeTrue("El permiso de organización debe coincidir con el recurso y la acción.");
        ownPermission.MatchesResource("Users", "Read").Should().BeTrue("El permiso propio debe coincidir con el recurso y la acción.");

        // Assert - Diferentes cadenas de permiso debido al alcance
        globalPermission.GetFullPermissionString().Should().Be("Users:Read:Global", "La cadena de permiso global debe ser correcta.");
        orgPermission.GetFullPermissionString().Should().Be("Users:Read:Organization", "La cadena de permiso de organización debe ser correcta.");
        ownPermission.GetFullPermissionString().Should().Be("Users:Read:Own", "La cadena de permiso propio debe ser correcta.");

        // Assert - Diferentes niveles de riesgo
        globalPermission.RiskLevel.Should().Be(1, "El nivel de riesgo del permiso global debe ser 1."); // Permiso de sistema
        orgPermission.RiskLevel.Should().Be(5, "El nivel de riesgo del permiso de organización debe ser 5."); // Permiso personalizado
        ownPermission.RiskLevel.Should().Be(5, "El nivel de riesgo del permiso propio debe ser 5."); // Permiso personalizado
    }

    [Fact]
    public void PermisoExpirado_NoDeberiaSerValidoParaUsuario()
    {
        // Arrange
        var permission = CreateValidPermission();
        permission.IsActive = true;
        var user = CreateValidUser();

        // Inicialmente válido
        permission.IsValidForUser(user).Should().BeTrue("El permiso debe ser válido inicialmente.");

        // Act - Expirar el permiso
        permission.ValidUntil = DateTime.UtcNow.AddMinutes(-1);

        // Assert - Ya no debe ser válido
        permission.IsExpired().Should().BeTrue("El permiso debe estar expirado.");
        permission.IsValidForUser(user).Should().BeFalse("El permiso expirado no debe ser válido para el usuario.");
    }

    #endregion
}