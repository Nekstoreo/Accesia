namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class SecurityAuditLogTests
{
    #region Helpers para pruebas
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

    private static DeviceInfo CreateValidDeviceInfo()
    {
        return new DeviceInfo(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            DeviceType.Desktop,
            "Chrome",
            "119.0",
            "Windows 10",
            "device123");
    }

    private static LocationInfo CreateValidLocationInfo()
    {
        return new LocationInfo(
            "192.168.1.1",
            "Colombia",
            "Bogotá",
            "Cundinamarca",
            "Test ISP",
            false);
    }

    private static SecurityAuditLog CreateValidSecurityAuditLog(
        Guid? userId = null,
        string eventType = "TestEvent",
        bool isSuccessful = true)
    {
        return new SecurityAuditLog(
            userId ?? Guid.NewGuid(),
            eventType,
            "TestCategory",
            "Test description",
            "192.168.1.1",
            "TestUserAgent",
            CreateValidDeviceInfo(),
            "/api/test",
            "POST",
            isSuccessful);
    }

    #endregion

    #region Pruebas de métodos de fábrica

    [Fact]
    public void SecurityAuditLog_ConParametrosValidos_DeberiaCrearCorrectamente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventType = "LoginAttempt";
        var eventCategory = "Authentication";
        var description = "Test login attempt";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 Test";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/auth/login";
        var httpMethod = "POST";
        var isSuccessful = true;
        var severity = "High";
        var requestId = "req123";
        var failureReason = "Invalid credentials";
        var responseStatusCode = 200;
        var locationInfo = CreateValidLocationInfo();
        var additionalData = new Dictionary<string, object> { ["key"] = "value" };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var auditLog = new SecurityAuditLog(
            userId, eventType, eventCategory, description, ipAddress, userAgent,
            deviceInfo, endpoint, httpMethod, isSuccessful, severity, requestId,
            failureReason, responseStatusCode, locationInfo, additionalData);
        var afterCreation = DateTime.UtcNow;

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.Id.Should().NotBe(Guid.Empty, "El Id no debería ser Guid.Empty.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be(eventType, "El EventType debe coincidir.");
        auditLog.EventCategory.Should().Be(eventCategory, "El EventCategory debe coincidir.");
        auditLog.Description.Should().Be(description, "La Description debe coincidir.");
        auditLog.IpAddress.Should().Be(ipAddress, "La IpAddress debe coincidir.");
        auditLog.UserAgent.Should().Be(userAgent, "El UserAgent debe coincidir.");
        auditLog.DeviceInfo.Should().Be(deviceInfo, "La DeviceInfo debe coincidir.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        auditLog.Endpoint.Should().Be(endpoint, "El Endpoint debe coincidir.");
        auditLog.HttpMethod.Should().Be(httpMethod, "El HttpMethod debe coincidir.");
        auditLog.RequestId.Should().Be(requestId, "El RequestId debe coincidir.");
        auditLog.IsSuccessful.Should().Be(isSuccessful, "IsSuccessful debe coincidir.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
        auditLog.ResponseStatusCode.Should().Be(responseStatusCode, "ResponseStatusCode debe coincidir.");
        auditLog.AdditionalData.Should().NotBeNull("AdditionalData no debería ser nulo.");
        auditLog.AdditionalData.Should().ContainKey("key", "AdditionalData debe contener la clave 'key'.");
        auditLog.AdditionalData["key"].Should().Be("value", "El valor de AdditionalData['key'] debe ser 'value'.");
        auditLog.Severity.Should().Be(severity, "La Severity debe coincidir.");
        auditLog.OccurredAt.Should().BeAfter(beforeCreation, "OccurredAt debe ser posterior a beforeCreation.");
        auditLog.OccurredAt.Should().BeBefore(afterCreation, "OccurredAt debe ser anterior a afterCreation.");
    }

    [Fact]
    public void SecurityAuditLog_ConParametrosMinimos_DeberiaCrearConValoresPorDefecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceInfo = CreateValidDeviceInfo();

        // Act
        var auditLog = new SecurityAuditLog(
            userId, "TestEvent", "TestCategory", "Test description",
            "192.168.1.1", "TestUserAgent", deviceInfo, "/api/test", "POST", true);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.Id.Should().NotBe(Guid.Empty, "El Id no debería ser Guid.Empty.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("TestEvent", "El EventType debe ser 'TestEvent'.");
        auditLog.EventCategory.Should().Be("TestCategory", "El EventCategory debe ser 'TestCategory'.");
        auditLog.Severity.Should().Be("Medium", "La Severity por defecto debe ser 'Medium'.");
        auditLog.RequestId.Should().BeNull("RequestId debe ser nulo.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.ResponseStatusCode.Should().BeNull("ResponseStatusCode debe ser nulo.");
        auditLog.LocationInfo.Should().BeNull("LocationInfo debe ser nulo.");
        auditLog.AdditionalData.Should().NotBeNull("AdditionalData no debería ser nulo.");
        auditLog.AdditionalData.Should().BeEmpty("AdditionalData debe estar vacío.");
    }

    [Fact]
    public void SecurityAuditLog_ConUserIdNulo_DeberiaAceptarUsuarioNulo()
    {
        // Arrange & Act
        var auditLog = new SecurityAuditLog(
            null, "AnonymousEvent", "Security", "Anonymous activity",
            "192.168.1.1", "TestUserAgent", CreateValidDeviceInfo(), "/api/public", "GET", true);

        // Assert
        auditLog.UserId.Should().BeNull("El UserId debe ser nulo.");
        auditLog.EventType.Should().Be("AnonymousEvent", "El EventType debe ser 'AnonymousEvent'.");
    }

    [Fact]
    public void SecurityAuditLog_DeberiaGenerarIdsUnicos()
    {
        // Arrange
        var deviceInfo = CreateValidDeviceInfo();

        // Act
        var auditLog1 = new SecurityAuditLog(
            Guid.NewGuid(), "Event1", "Category", "Description",
            "192.168.1.1", "UserAgent", deviceInfo, "/api/test", "POST", true);
        var auditLog2 = new SecurityAuditLog(
            Guid.NewGuid(), "Event2", "Category", "Description",
            "192.168.1.1", "UserAgent", deviceInfo, "/api/test", "POST", true);

        // Assert
        auditLog1.Id.Should().NotBe(auditLog2.Id, "Los IDs deben ser únicos.");
        auditLog1.Id.Should().NotBe(Guid.Empty, "El primer Id no debería ser Guid.Empty.");
        auditLog2.Id.Should().NotBe(Guid.Empty, "El segundo Id no debería ser Guid.Empty.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Intento de inicio de sesión

    [Fact]
    public void CreateLoginAttempt_LoginExitoso_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreateLoginAttempt(
            userId, email, ipAddress, userAgent, deviceInfo, true, locationInfo: locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("LoginAttempt", "El EventType debe ser LoginAttempt.");
        auditLog.EventCategory.Should().Be("Authentication", "El EventCategory debe ser Authentication.");
        auditLog.Description.Should().Contain("Login exitoso").And.Contain(email, "La descripción debe indicar éxito y contener el email.");
        auditLog.IpAddress.Should().Be(ipAddress, "La IpAddress debe coincidir.");
        auditLog.UserAgent.Should().Be(userAgent, "El UserAgent debe coincidir.");
        auditLog.DeviceInfo.Should().Be(deviceInfo, "La DeviceInfo debe coincidir.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        auditLog.Endpoint.Should().Be("/api/auth/login", "El Endpoint debe ser /api/auth/login.");
        auditLog.HttpMethod.Should().Be("POST", "El HttpMethod debe ser POST.");
        auditLog.IsSuccessful.Should().BeTrue("IsSuccessful debe ser true.");
        auditLog.Severity.Should().Be("Low", "La Severity debe ser Low para logins exitosos.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo para logins exitosos.");
        auditLog.AdditionalData.Should().ContainKey("email", "AdditionalData debe contener la clave email.");
        auditLog.AdditionalData.Should().ContainKey("attemptedUserId", "AdditionalData debe contener la clave attemptedUserId.");
        auditLog.AdditionalData["email"].Should().Be(email, "El email en AdditionalData debe coincidir.");
        auditLog.AdditionalData["attemptedUserId"].Should().Be(userId.ToString(), "El attemptedUserId en AdditionalData debe coincidir.");
    }

    [Fact]
    public void CreateLoginAttempt_LoginFallido_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var failureReason = "Invalid password";

        // Act
        var auditLog = SecurityAuditLog.CreateLoginAttempt(
            null, email, ipAddress, userAgent, deviceInfo, false, failureReason);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().BeNull("El UserId debe ser nulo para login fallido si no se proporciona.");
        auditLog.EventType.Should().Be("LoginAttempt", "El EventType debe ser LoginAttempt.");
        auditLog.EventCategory.Should().Be("Authentication", "El EventCategory debe ser Authentication.");
        auditLog.Description.Should().Contain("Login fallido").And.Contain(email).And.Contain(failureReason, "La descripción debe indicar fallo, contener email y razón.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High para logins fallidos.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
        auditLog.AdditionalData["email"].Should().Be(email, "El email en AdditionalData debe coincidir.");
        auditLog.AdditionalData["attemptedUserId"].Should().Be("unknown", "El attemptedUserId en AdditionalData debe ser 'unknown'.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Cambio de contraseña

    [Fact]
    public void CreatePasswordChange_CambioExitoso_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreatePasswordChange(
            userId, ipAddress, userAgent, deviceInfo, true, locationInfo: locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("PasswordChange", "El EventType debe ser PasswordChange.");
        auditLog.EventCategory.Should().Be("AccountSecurity", "El EventCategory debe ser AccountSecurity.");
        auditLog.Description.Should().Be("Contraseña cambiada exitosamente", "La descripción debe indicar cambio de contraseña exitoso.");
        auditLog.Endpoint.Should().Be("/api/auth/change-password", "El Endpoint debe ser /api/auth/change-password.");
        auditLog.HttpMethod.Should().Be("POST", "El HttpMethod debe ser POST.");
        auditLog.IsSuccessful.Should().BeTrue("IsSuccessful debe ser true.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
    }

    [Fact]
    public void CreatePasswordChange_CambioFallido_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var failureReason = "Current password incorrect";

        // Act
        var auditLog = SecurityAuditLog.CreatePasswordChange(
            userId, ipAddress, userAgent, deviceInfo, false, failureReason);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("PasswordChange", "El EventType debe ser PasswordChange.");
        auditLog.Description.Should().Contain("Cambio de contraseña fallido").And.Contain(failureReason, "La descripción debe indicar fallo y razón.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Cambio de correo electrónico

    [Fact]
    public void CreateEmailChange_CambioExitoso_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldEmail = "old@example.com";
        var newEmail = "new@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreateEmailChange(
            userId, oldEmail, newEmail, ipAddress, userAgent, deviceInfo, true, locationInfo: locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("EmailChange", "El EventType debe ser EmailChange.");
        auditLog.EventCategory.Should().Be("AccountSecurity", "El EventCategory debe ser AccountSecurity.");
        auditLog.Description.Should().Contain("Email cambiado de").And.Contain(oldEmail).And.Contain(newEmail, "La descripción debe indicar cambio de email exitoso con emails antiguo y nuevo.");
        auditLog.Endpoint.Should().Be("/api/users/change-email", "El Endpoint debe ser /api/users/change-email.");
        auditLog.HttpMethod.Should().Be("POST", "El HttpMethod debe ser POST.");
        auditLog.IsSuccessful.Should().BeTrue("IsSuccessful debe ser true.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        auditLog.AdditionalData.Should().ContainKey("oldEmail", "AdditionalData debe contener la clave oldEmail.");
        auditLog.AdditionalData.Should().ContainKey("newEmail", "AdditionalData debe contener la clave newEmail.");
        auditLog.AdditionalData["oldEmail"].Should().Be(oldEmail, "El oldEmail en AdditionalData debe coincidir.");
        auditLog.AdditionalData["newEmail"].Should().Be(newEmail, "El newEmail en AdditionalData debe coincidir.");
    }

    [Fact]
    public void CreateEmailChange_CambioFallido_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldEmail = "old@example.com";
        var newEmail = "new@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var failureReason = "Email already in use";

        // Act
        var auditLog = SecurityAuditLog.CreateEmailChange(
            userId, oldEmail, newEmail, ipAddress, userAgent, deviceInfo, false, failureReason);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("EmailChange", "El EventType debe ser EmailChange.");
        auditLog.Description.Should().Contain("Cambio de email fallido").And.Contain(failureReason, "La descripción debe indicar fallo y razón.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
        auditLog.AdditionalData["oldEmail"].Should().Be(oldEmail, "El oldEmail en AdditionalData debe coincidir.");
        auditLog.AdditionalData["newEmail"].Should().Be(newEmail, "El newEmail en AdditionalData debe coincidir.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Eliminación de cuenta

    [Fact]
    public void CreateAccountDeletion_EliminacionExitosa_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreateAccountDeletion(
            userId, ipAddress, userAgent, deviceInfo, true, locationInfo: locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("AccountDeletion", "El EventType debe ser AccountDeletion.");
        auditLog.EventCategory.Should().Be("AccountSecurity", "El EventCategory debe ser AccountSecurity.");
        auditLog.Description.Should().Be("Solicitud de eliminación de cuenta procesada", "La descripción debe indicar eliminación procesada.");
        auditLog.Endpoint.Should().Be("/api/users/request-account-deletion", "El Endpoint debe ser /api/users/request-account-deletion.");
        auditLog.HttpMethod.Should().Be("POST", "El HttpMethod debe ser POST.");
        auditLog.IsSuccessful.Should().BeTrue("IsSuccessful debe ser true.");
        auditLog.Severity.Should().Be("Critical", "La Severity debe ser Critical.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
    }

    [Fact]
    public void CreateAccountDeletion_EliminacionFallida_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var failureReason = "Account has pending transactions";

        // Act
        var auditLog = SecurityAuditLog.CreateAccountDeletion(
            userId, ipAddress, userAgent, deviceInfo, false, failureReason);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("AccountDeletion", "El EventType debe ser AccountDeletion.");
        auditLog.Description.Should().Contain("Solicitud de eliminación fallida").And.Contain(failureReason, "La descripción debe indicar fallo y razón.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("Critical", "La Severity debe ser Critical.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Excedido límite de tasa

    [Fact]
    public void CreateRateLimitExceeded_ConUserId_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/auth/login";
        var policyName = "LoginPolicy";
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreateRateLimitExceeded(
            userId, ipAddress, userAgent, deviceInfo, endpoint, policyName, locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("RateLimitExceeded", "El EventType debe ser RateLimitExceeded.");
        auditLog.EventCategory.Should().Be("Security", "El EventCategory debe ser Security.");
        auditLog.Description.Should().Contain("Rate limit excedido").And.Contain(policyName).And.Contain(endpoint, "La descripción debe indicar el límite excedido, política y endpoint.");
        auditLog.Endpoint.Should().Be(endpoint, "El Endpoint debe coincidir.");
        auditLog.HttpMethod.Should().Be("ANY", "El HttpMethod debe ser ANY.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.FailureReason.Should().Contain("Rate limit policy").And.Contain(policyName, "FailureReason debe indicar la política de límite.");
        auditLog.ResponseStatusCode.Should().Be(429, "ResponseStatusCode debe ser 429.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        auditLog.AdditionalData.Should().ContainKey("policyName", "AdditionalData debe contener la clave policyName.");
        auditLog.AdditionalData.Should().ContainKey("limitType", "AdditionalData debe contener la clave limitType.");
        auditLog.AdditionalData["policyName"].Should().Be(policyName, "El policyName en AdditionalData debe coincidir.");
        auditLog.AdditionalData["limitType"].Should().Be("RateLimit", "El limitType en AdditionalData debe ser 'RateLimit'.");
    }

    [Fact]
    public void CreateRateLimitExceeded_SinUserId_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/public/register";
        var policyName = "RegisterPolicy";

        // Act
        var auditLog = SecurityAuditLog.CreateRateLimitExceeded(
            null, ipAddress, userAgent, deviceInfo, endpoint, policyName);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().BeNull("El UserId debe ser nulo.");
        auditLog.EventType.Should().Be("RateLimitExceeded", "El EventType debe ser RateLimitExceeded.");
        auditLog.Description.Should().Contain("Rate limit excedido").And.Contain(policyName).And.Contain(endpoint, "La descripción debe indicar límite excedido, política y endpoint.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.ResponseStatusCode.Should().Be(429, "ResponseStatusCode debe ser 429.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Actividad sospechosa o inusual

    [Fact]
    public void CreateSuspiciousActivity_ConTodosLosParametros_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/users/profile";
        var activityDescription = "Multiple rapid profile updates from different locations";
        var failureReason = "Suspicious pattern detected";
        var locationInfo = CreateValidLocationInfo();
        var additionalData = new Dictionary<string, object>
        {
            ["updateCount"] = 10,
            ["timeWindow"] = 60
        };

        // Act
        var auditLog = SecurityAuditLog.CreateSuspiciousActivity(
            userId, ipAddress, userAgent, deviceInfo, endpoint, activityDescription,
            failureReason, locationInfo, additionalData);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("SuspiciousActivity", "El EventType debe ser SuspiciousActivity.");
        auditLog.EventCategory.Should().Be("Security", "El EventCategory debe ser Security.");
        auditLog.Description.Should().Contain("Actividad sospechosa detectada").And.Contain(activityDescription, "La descripción debe indicar actividad sospechosa.");
        auditLog.Endpoint.Should().Be(endpoint, "El Endpoint debe coincidir.");
        auditLog.HttpMethod.Should().Be("ANY", "El HttpMethod debe ser ANY.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("Critical", "La Severity debe ser Critical.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        auditLog.AdditionalData.Should().ContainKey("updateCount", "AdditionalData debe contener updateCount.");
        auditLog.AdditionalData.Should().ContainKey("timeWindow", "AdditionalData debe contener timeWindow.");
        auditLog.AdditionalData["updateCount"].Should().Be(10, "El updateCount en AdditionalData debe ser 10.");
        auditLog.AdditionalData["timeWindow"].Should().Be(60, "El timeWindow en AdditionalData debe ser 60.");
    }

    [Fact]
    public void CreateSuspiciousActivity_ConParametrosMinimos_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/test";
        var activityDescription = "Unusual access pattern";

        // Act
        var auditLog = SecurityAuditLog.CreateSuspiciousActivity(
            null, ipAddress, userAgent, deviceInfo, endpoint, activityDescription);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().BeNull("El UserId debe ser nulo.");
        auditLog.EventType.Should().Be("SuspiciousActivity", "El EventType debe ser SuspiciousActivity.");
        auditLog.Description.Should().Contain("Actividad sospechosa detectada").And.Contain(activityDescription, "La descripción debe indicar actividad sospechosa.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("Critical", "La Severity debe ser Critical.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.LocationInfo.Should().BeNull("LocationInfo debe ser nulo.");
        auditLog.AdditionalData.Should().BeEmpty("AdditionalData debe estar vacío.");
    }

    #endregion

    #region Pruebas de métodos de fábrica - Acceso no autorizado

    [Fact]
    public void CreateUnauthorizedAccess_ConTodosLosParametros_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/admin/users";
        var httpMethod = "DELETE";
        var failureReason = "Insufficient permissions";
        var locationInfo = CreateValidLocationInfo();

        // Act
        var auditLog = SecurityAuditLog.CreateUnauthorizedAccess(
            userId, ipAddress, userAgent, deviceInfo, endpoint, httpMethod, failureReason, locationInfo);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().Be(userId, "El UserId debe coincidir.");
        auditLog.EventType.Should().Be("UnauthorizedAccess", "El EventType debe ser UnauthorizedAccess.");
        auditLog.EventCategory.Should().Be("Security", "El EventCategory debe ser Security.");
        auditLog.Description.Should().Contain("Intento de acceso no autorizado a").And.Contain(endpoint, "La descripción debe indicar acceso no autorizado.");
        auditLog.Endpoint.Should().Be(endpoint, "El Endpoint debe coincidir.");
        auditLog.HttpMethod.Should().Be(httpMethod, "El HttpMethod debe coincidir.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().Be(failureReason, "FailureReason debe coincidir.");
        auditLog.ResponseStatusCode.Should().Be(401, "ResponseStatusCode debe ser 401.");
        auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
    }

    [Fact]
    public void CreateUnauthorizedAccess_ConParametrosMinimos_DeberiaCrearLogCorrecto()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        var endpoint = "/api/protected";
        var httpMethod = "GET";

        // Act
        var auditLog = SecurityAuditLog.CreateUnauthorizedAccess(
            null, ipAddress, userAgent, deviceInfo, endpoint, httpMethod);

        // Assert
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
        auditLog.UserId.Should().BeNull("El UserId debe ser nulo.");
        auditLog.EventType.Should().Be("UnauthorizedAccess", "El EventType debe ser UnauthorizedAccess.");
        auditLog.Description.Should().Contain("Intento de acceso no autorizado a").And.Contain(endpoint, "La descripción debe indicar acceso no autorizado.");
        auditLog.IsSuccessful.Should().BeFalse("IsSuccessful debe ser false.");
        auditLog.Severity.Should().Be("High", "La Severity debe ser High.");
        auditLog.FailureReason.Should().BeNull("FailureReason debe ser nulo.");
        auditLog.ResponseStatusCode.Should().Be(401, "ResponseStatusCode debe ser 401.");
        auditLog.LocationInfo.Should().BeNull("LocationInfo debe ser nulo.");
    }

    #endregion

    #region Pruebas de gestión de datos adicionales

    [Fact]
    public void AddAdditionalData_ConClaveValor_DeberiaAñadirAColeccion()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var key = "testKey";
        var value = "testValue";

        // Act
        auditLog.AddAdditionalData(key, value);

        // Assert
        auditLog.AdditionalData.Should().ContainKey(key, "AdditionalData debe contener la clave.");
        auditLog.AdditionalData[key].Should().Be(value, "El valor de la clave debe coincidir.");
    }

    [Fact]
    public void AddAdditionalData_ConDiccionario_DeberiaAñadirTodoAColeccion()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var dataToAdd = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = DateTime.UtcNow,
            ["key4"] = true
        };

        // Act
        auditLog.AddAdditionalData(dataToAdd);

        // Assert
        auditLog.AdditionalData.Should().ContainKey("key1", "AdditionalData debe contener key1.");
        auditLog.AdditionalData.Should().ContainKey("key2", "AdditionalData debe contener key2.");
        auditLog.AdditionalData.Should().ContainKey("key3", "AdditionalData debe contener key3.");
        auditLog.AdditionalData.Should().ContainKey("key4", "AdditionalData debe contener key4.");
        auditLog.AdditionalData["key1"].Should().Be("value1", "El valor de key1 debe ser 'value1'.");
        auditLog.AdditionalData["key2"].Should().Be(42, "El valor de key2 debe ser 42.");
        auditLog.AdditionalData["key3"].Should().Be(dataToAdd["key3"], "El valor de key3 debe coincidir.");
        auditLog.AdditionalData["key4"].Should().Be(true, "El valor de key4 debe ser true.");
    }

    [Fact]
    public void AddAdditionalData_ConClaveExistente_DeberiaSobrescribirValor()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var key = "testKey";
        var originalValue = "originalValue";
        var newValue = "newValue";

        // Act
        auditLog.AddAdditionalData(key, originalValue);
        auditLog.AddAdditionalData(key, newValue);

        // Assert
        auditLog.AdditionalData.Should().ContainKey(key, "AdditionalData debe contener la clave.");
        auditLog.AdditionalData[key].Should().Be(newValue, "El valor de la clave debe ser el nuevo valor.");
        auditLog.AdditionalData.Should().HaveCount(1, "AdditionalData debe tener solo un elemento.");
    }

    [Fact]
    public void AddAdditionalData_ConObjetosComplejos_DeberiaAlmacenarCorrectamente()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var complexObject = new { Name = "Test", Value = 123, IsActive = true };
        var array = new[] { 1, 2, 3, 4, 5 };
        var nestedDict = new Dictionary<string, object> { ["nested"] = "value" };

        // Act
        auditLog.AddAdditionalData("complex", complexObject);
        auditLog.AddAdditionalData("array", array);
        auditLog.AddAdditionalData("nested", nestedDict);

        // Assert
        auditLog.AdditionalData["complex"].Should().Be(complexObject, "El objeto complejo debe almacenarse correctamente.");
        auditLog.AdditionalData["array"].Should().Be(array, "El array debe almacenarse correctamente.");
        auditLog.AdditionalData["nested"].Should().Be(nestedDict, "El diccionario anidado debe almacenarse correctamente.");
    }

    #endregion

    #region Pruebas de validación de propiedades

    [Fact]
    public void SecurityAuditLog_PropiedadesDeberianTenerSetPrivado()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var type = typeof(SecurityAuditLog);

        // Act & Assert - Las propiedades deberían tener setters privados
        var properties = new[]
        {
            nameof(SecurityAuditLog.Id),
            nameof(SecurityAuditLog.UserId),
            nameof(SecurityAuditLog.EventType),
            nameof(SecurityAuditLog.EventCategory),
            nameof(SecurityAuditLog.Description),
            nameof(SecurityAuditLog.IpAddress),
            nameof(SecurityAuditLog.UserAgent),
            nameof(SecurityAuditLog.DeviceInfo),
            nameof(SecurityAuditLog.LocationInfo),
            nameof(SecurityAuditLog.Endpoint),
            nameof(SecurityAuditLog.HttpMethod),
            nameof(SecurityAuditLog.RequestId),
            nameof(SecurityAuditLog.IsSuccessful),
            nameof(SecurityAuditLog.FailureReason),
            nameof(SecurityAuditLog.ResponseStatusCode),
            nameof(SecurityAuditLog.Severity),
            nameof(SecurityAuditLog.OccurredAt)
        };

        foreach (var propertyName in properties)
        {
            var property = type.GetProperty(propertyName);
            property.Should().NotBeNull($"La propiedad {propertyName} debería existir");
            property!.SetMethod.Should().NotBeNull($"La propiedad {propertyName} debería tener un setter");
            property.SetMethod!.IsPrivate.Should().BeTrue($"La propiedad {propertyName} debería tener un setter privado");
        }
    }

    [Fact]
    public void SecurityAuditLog_PropiedadAdditionalData_DeberiaSerColeccionDeSoloLectura()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();

        // Act
        var property = typeof(SecurityAuditLog).GetProperty(nameof(SecurityAuditLog.AdditionalData));

        // Assert
        property.Should().NotBeNull("La propiedad AdditionalData no debe ser nula.");
        property!.SetMethod.Should().BeNull("AdditionalData no debería tener un setter");
        auditLog.AdditionalData.Should().NotBeNull("AdditionalData no debería ser nulo.");
        auditLog.AdditionalData.Should().BeAssignableTo<Dictionary<string, object>>("AdditionalData debe ser asignable a Dictionary<string, object>.");
    }

    [Theory]
    [InlineData(true, "Low")]
    [InlineData(false, "High")]
    public void SecurityAuditLog_SeveridadBasadaEnExito_DeberiaSerConsistente(bool isSuccessful, string expectedSeverity)
    {
        // Arrange & Act
        var auditLog = SecurityAuditLog.CreateLoginAttempt(
            Guid.NewGuid(), "test@example.com", "192.168.1.1", "Mozilla/5.0",
            CreateValidDeviceInfo(), isSuccessful);

        // Assert
        auditLog.Severity.Should().Be(expectedSeverity, $"La severidad debe ser {expectedSeverity} para isSuccessful = {isSuccessful}.");
    }

    #endregion

    #region Pruebas de propiedades de navegación

    [Fact]
    public void SecurityAuditLog_DeberiaTenerPropiedadDeNavegacionUser()
    {
        // Arrange & Act
        var auditLog = CreateValidSecurityAuditLog();
        var user = CreateValidUser();

        // Assert
        auditLog.User.Should().BeNull("El User debe ser nulo inicialmente."); // Initially null
        
        // Act - Set user
        auditLog.User = user;
        
        // Assert
        auditLog.User.Should().Be(user, "El User debe coincidir con el usuario establecido.");
    }

    #endregion

    #region Pruebas de entidad auditable

    [Fact]
    public void SecurityAuditLog_DeberiaHeredarDeAuditableEntity()
    {
        // Arrange & Act
        var auditLog = CreateValidSecurityAuditLog();

        // Assert
        auditLog.Should().BeAssignableTo<AuditableEntity>("SecurityAuditLog debe heredar de AuditableEntity.");
        auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");

        // El Id debería ser sobreescrito y establecido por el constructor
        auditLog.Id.Should().NotBe(Guid.Empty, "El Id no debería ser Guid.Empty después de la construcción.");
    }

    [Fact]
    public void SecurityAuditLog_DeberiaTenerPropiedadesAuditables()
    {
        // Arrange & Act
        var auditLog = CreateValidSecurityAuditLog();

        // Assert - Probar que las propiedades auditables existen y pueden ser establecidas
        var now = DateTime.UtcNow;
        var createdBy = "test-user-id";
        
        auditLog.CreatedAt = now;
        auditLog.CreatedBy = createdBy;
        
        auditLog.CreatedAt.Should().Be(now, "CreatedAt debería ser la fecha actual.");
        auditLog.CreatedBy.Should().Be(createdBy, "CreatedBy debería ser el usuario de prueba.");
    }

    #endregion

    #region Pruebas de integración

    [Fact]
    public void SecurityAuditLogLifecycle_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var user = CreateValidUser();
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();

        // Act - Crear log de auditoría con diferentes métodos de fábrica
        var loginAttempt = SecurityAuditLog.CreateLoginAttempt(
            user.Id, user.Email.Value, "192.168.1.1", "Mozilla/5.0", deviceInfo, true, locationInfo: locationInfo);

        var passwordChange = SecurityAuditLog.CreatePasswordChange(
            user.Id, "192.168.1.2", "Mozilla/5.0", deviceInfo, true, locationInfo: locationInfo);

        var emailChange = SecurityAuditLog.CreateEmailChange(
            user.Id, "old@example.com", "new@example.com", "192.168.1.3", 
            "Mozilla/5.0", deviceInfo, true, locationInfo: locationInfo);

        // Assert - Todos deben tener propiedades válidas
        var auditLogs = new[] { loginAttempt, passwordChange, emailChange };
        foreach (var auditLog in auditLogs)
        {
            auditLog.Should().NotBeNull("El log de auditoría no debería ser nulo.");
            auditLog.Id.Should().NotBe(Guid.Empty, "El Id no debería ser Guid.Empty.");
            auditLog.UserId.Should().Be(user.Id, "El UserId debe coincidir.");
            auditLog.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "OccurredAt debe ser cercano a la hora actual.");
            auditLog.DeviceInfo.Should().Be(deviceInfo, "La DeviceInfo debe coincidir.");
            auditLog.LocationInfo.Should().Be(locationInfo, "La LocationInfo debe coincidir.");
        }

        // Assert - Cada uno debe tener IDs únicos
        var ids = auditLogs.Select(al => al.Id).ToList();
        ids.Should().OnlyHaveUniqueItems("Todos los logs de auditoría deben tener IDs únicos.");

        // Act - Agregar datos adicionales
        loginAttempt.AddAdditionalData("sessionId", "session123");
        passwordChange.AddAdditionalData(new Dictionary<string, object> { ["strength"] = "Strong" });

        // Assert - Los datos adicionales deben estar presentes
        loginAttempt.AdditionalData.Should().ContainKey("sessionId", "LoginAttempt debe contener sessionId.");
        passwordChange.AdditionalData.Should().ContainKey("strength", "PasswordChange debe contener strength.");

        // Act - Establecer propiedades de navegación
        loginAttempt.User = user;
        passwordChange.User = user;
        emailChange.User = user;

        // Assert - Las propiedades de navegación deben estar establecidas
        foreach (var auditLog in auditLogs)
        {
            auditLog.User.Should().Be(user, "El User debe coincidir con el usuario establecido.");
            auditLog.User!.Id.Should().Be(auditLog.UserId.Value, "El Id del usuario debe coincidir con el UserId del log.");
        }
    }

    [Theory]
    [InlineData("LoginAttempt", "Authentication", "Low")]
    [InlineData("PasswordChange", "AccountSecurity", "High")]
    [InlineData("EmailChange", "AccountSecurity", "High")]
    [InlineData("AccountDeletion", "AccountSecurity", "Critical")]
    [InlineData("RateLimitExceeded", "Security", "Medium")]
    [InlineData("SuspiciousActivity", "Security", "Critical")]
    [InlineData("UnauthorizedAccess", "Security", "High")]
    public void MetodosDeFabrica_DeberianTenerTiposDeEventoYCategoriasConsistentes(
        string eventType, string expectedCategory, string expectedSeverity)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var deviceInfo = CreateValidDeviceInfo();
        SecurityAuditLog auditLog;

        // Act
        switch (eventType)
        {
            case "LoginAttempt":
                auditLog = SecurityAuditLog.CreateLoginAttempt(
                    userId, "test@example.com", ipAddress, userAgent, deviceInfo, true);
                break;
            case "PasswordChange":
                auditLog = SecurityAuditLog.CreatePasswordChange(
                    userId, ipAddress, userAgent, deviceInfo, true);
                break;
            case "EmailChange":
                auditLog = SecurityAuditLog.CreateEmailChange(
                    userId, "old@example.com", "new@example.com", ipAddress, userAgent, deviceInfo, true);
                break;
            case "AccountDeletion":
                auditLog = SecurityAuditLog.CreateAccountDeletion(
                    userId, ipAddress, userAgent, deviceInfo, true);
                break;
            case "RateLimitExceeded":
                auditLog = SecurityAuditLog.CreateRateLimitExceeded(
                    userId, ipAddress, userAgent, deviceInfo, "/api/test", "TestPolicy");
                break;
            case "SuspiciousActivity":
                auditLog = SecurityAuditLog.CreateSuspiciousActivity(
                    userId, ipAddress, userAgent, deviceInfo, "/api/test", "Test activity");
                break;
            case "UnauthorizedAccess":
                auditLog = SecurityAuditLog.CreateUnauthorizedAccess(
                    userId, ipAddress, userAgent, deviceInfo, "/api/test", "GET");
                break;
            default:
                throw new ArgumentException($"Unknown event type: {eventType}");
        }

        // Assert
        auditLog.EventType.Should().Be(eventType, "El EventType debe coincidir.");
        auditLog.EventCategory.Should().Be(expectedCategory, "El EventCategory debe coincidir.");
        auditLog.Severity.Should().Be(expectedSeverity, "La Severity debe coincidir.");
    }

    [Fact]
    public void SecurityAuditLog_PrecisionDeTimestamp_DeberiaSerExacta()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var auditLog1 = CreateValidSecurityAuditLog();
        Thread.Sleep(10); // Pequeño retraso para asegurar diferentes marcas de tiempo
        var auditLog2 = CreateValidSecurityAuditLog();
        
        var afterCreation = DateTime.UtcNow;

        // Assert
        auditLog1.OccurredAt.Should().BeAfter(beforeCreation, "El primer log debe ser posterior a beforeCreation.");
        auditLog1.OccurredAt.Should().BeBefore(afterCreation, "El primer log debe ser anterior a afterCreation.");
        auditLog2.OccurredAt.Should().BeAfter(beforeCreation, "El segundo log debe ser posterior a beforeCreation.");
        auditLog2.OccurredAt.Should().BeBefore(afterCreation, "El segundo log debe ser anterior a afterCreation.");
        auditLog2.OccurredAt.Should().BeOnOrAfter(auditLog1.OccurredAt, "El segundo log debe ser igual o posterior al primero.");
    }

    [Fact]
    public void SecurityAuditLog_ConAdditionalDataGrande_DeberiaManejarCorrectamente()
    {
        // Arrange
        var auditLog = CreateValidSecurityAuditLog();
        var largeData = new Dictionary<string, object>();
        
        // Agregar 100 claves diferentes con varios tipos de datos
        for (int i = 0; i < 100; i++)
        {
            largeData[$"key_{i}"] = new
            {
                Index = i,
                Name = $"Item_{i}",
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                IsActive = i % 2 == 0,
                Data = new[] { i, i * 2, i * 3 }
            };
        }

        // Act
        auditLog.AddAdditionalData(largeData);

        // Assert
        auditLog.AdditionalData.Should().HaveCount(100, "AdditionalData debe contener 100 elementos.");
        auditLog.AdditionalData.Should().ContainKey("key_0", "AdditionalData debe contener la clave key_0.");
        auditLog.AdditionalData.Should().ContainKey("key_99", "AdditionalData debe contener la clave key_99.");
        
        // Verificar integridad de datos específicos
        var firstItem = auditLog.AdditionalData["key_0"];
        firstItem.Should().NotBeNull("El primer elemento no debería ser nulo.");
        
        var lastItem = auditLog.AdditionalData["key_99"];
        lastItem.Should().NotBeNull("El último elemento no debería ser nulo.");
    }

    #endregion
}