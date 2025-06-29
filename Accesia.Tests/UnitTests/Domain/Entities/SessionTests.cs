namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class SessionTests
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
            "91.0.4472.124",
            "Windows 10",
            "a1b2c3d4e5f6789"
        );
    }

    private static LocationInfo CreateValidLocationInfo(string ipAddress = "192.168.1.1")
    {
        return new LocationInfo(ipAddress, "Colombia", "Bogotá");
    }

    private static Session CreateValidSession(
        User? user = null,
        DeviceInfo? deviceInfo = null,
        LocationInfo? locationInfo = null,
        SessionStatus status = SessionStatus.Active)
    {
        user ??= CreateValidUser();
        deviceInfo ??= CreateValidDeviceInfo();
        locationInfo ??= CreateValidLocationInfo();

        return new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            Status = status,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            LastActivityAt = DateTime.UtcNow,
            DeviceInfo = deviceInfo,
            LocationInfo = locationInfo,
            IsKnownDevice = false,
            LoginMethod = LoginMethod.Password,
            MfaVerified = false,
            TwoFactorRequired = false,
            RiskScore = 0,
            UserAgent = deviceInfo.UserAgent,
            InitialIpAddress = locationInfo.IpAddress,
            LastIpAddress = locationInfo.IpAddress,
            User = user
        };
    }

    #endregion

    #region Pruebas de métodos de fábrica

    [Fact]
    public void CreateNewSession_DeberiaCrearSesionDeContraseñaValida()
    {
        // Arrange
        var user = CreateValidUser();
        var device = CreateValidDeviceInfo();
        var location = CreateValidLocationInfo();
        var loginMethod = "password";

        // Act
        var session = Session.CreateNewSession(user, device, location, loginMethod);

        // Assert
        session.Should().NotBeNull("La sesión no debe ser nula después de la creación.");
        session.UserId.Should().Be(user.Id, "El UserId de la sesión debe coincidir con el del usuario.");
        session.SessionToken.Should().NotBeNullOrEmpty("El SessionToken no debe ser nulo ni vacío.");
        session.RefreshToken.Should().NotBeNullOrEmpty("El RefreshToken no debe ser nulo ni vacío.");
        session.Status.Should().Be(SessionStatus.Active, "El estado inicial de la sesión debe ser Activo.");
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "La fecha de expiración de la sesión debe ser en el futuro.");
        session.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow, "La fecha de expiración del RefreshToken debe ser en el futuro.");
        session.DeviceInfo.Should().Be(device, "La información del dispositivo debe coincidir.");
        session.LocationInfo.Should().Be(location, "La información de ubicación debe coincidir.");
        session.LoginMethod.Should().Be(LoginMethod.Password, "El método de login debe ser Password.");
        session.IsKnownDevice.Should().BeFalse("IsKnownDevice debe ser falso para una sesión nueva.");
        session.MfaVerified.Should().BeFalse("MfaVerified debe ser falso para una sesión de contraseña inicial.");
        session.TwoFactorRequired.Should().BeFalse("TwoFactorRequired debe ser falso inicialmente.");
        session.RiskScore.Should().Be(0, "El RiskScore debe ser 0 para una sesión nueva.");
        session.InitialIpAddress.Should().Be(location.IpAddress, "La IP inicial debe coincidir con la ubicación.");
        session.LastIpAddress.Should().Be(location.IpAddress, "La última IP debe coincidir con la ubicación.");
    }

    [Fact]
    public void CreateFromOAuth_DeberiaCrearSesionOAuthValida()
    {
        // Arrange
        var user = CreateValidUser();
        var device = CreateValidDeviceInfo();
        var location = CreateValidLocationInfo();
        var provider = "Google";

        // Act
        var session = Session.CreateFromOAuth(user, device, location, provider);

        // Assert
        session.Should().NotBeNull("La sesión OAuth no debe ser nula.");
        session.UserId.Should().Be(user.Id, "El UserId de la sesión OAuth debe coincidir con el del usuario.");
        session.LoginMethod.Should().Be(LoginMethod.GoogleOAuth, "El método de login debe ser GoogleOAuth.");
        session.Status.Should().Be(SessionStatus.Active, "El estado de la sesión OAuth debe ser Activo.");
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "La fecha de expiración de la sesión OAuth debe ser en el futuro.");
        session.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow, "La fecha de expiración del RefreshToken OAuth debe ser en el futuro.");
        session.MfaVerified.Should().BeFalse("MfaVerified debe ser falso para una sesión OAuth inicial.");
        session.IsKnownDevice.Should().BeFalse("IsKnownDevice debe ser falso para una sesión OAuth nueva.");
    }

    [Fact]
    public void CreateMfaSession_DeberiaCrearSesionMfaValida()
    {
        // Arrange
        var user = CreateValidUser();
        var device = CreateValidDeviceInfo();
        var location = CreateValidLocationInfo();

        // Act
        var session = Session.CreateMfaSession(user, device, location);

        // Assert
        session.Should().NotBeNull("La sesión MFA no debe ser nula.");
        session.UserId.Should().Be(user.Id, "El UserId de la sesión MFA debe coincidir con el del usuario.");
        session.LoginMethod.Should().Be(LoginMethod.MFA, "El método de login debe ser MFA.");
        session.Status.Should().Be(SessionStatus.Active, "El estado de la sesión MFA debe ser Activo.");
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1), "La expiración de la sesión MFA debe ser aproximadamente 30 minutos en el futuro.");
        session.RefreshTokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromMinutes(5), "La expiración del RefreshToken MFA debe ser aproximadamente 2 horas en el futuro.");
        session.MfaVerified.Should().BeTrue("MfaVerified debe ser verdadero para una sesión MFA.");
        session.TwoFactorRequired.Should().BeFalse("TwoFactorRequired debe ser falso para una sesión MFA.");
    }

    [Theory]
    [InlineData("password", LoginMethod.Password)]
    [InlineData("oauth", LoginMethod.GoogleOAuth)]
    [InlineData("mfa", LoginMethod.MFA)]
    [InlineData("sso", LoginMethod.SAML)]
    [InlineData("unknown", LoginMethod.Password)] // Default case
    public void CreateNewSession_DeberiaAnalizarLoginMethodCorrectamente(string loginMethod, LoginMethod expectedMethod)
    {
        // Arrange
        var user = CreateValidUser();
        var device = CreateValidDeviceInfo();
        var location = CreateValidLocationInfo();

        // Act
        var session = Session.CreateNewSession(user, device, location, loginMethod);

        // Assert
        session.LoginMethod.Should().Be(expectedMethod, $"El método de login '{loginMethod}' debe ser mapeado correctamente a {expectedMethod}.");
    }

    #endregion

    #region Pruebas de gestión de estado

    [Fact]
    public void Activate_CuandoSesionNoEstaActiva_DeberiaActivarYActualizarActividad()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Suspended);
        var initialActivity = session.LastActivityAt;

        // Act
        session.Activate();

        // Assert
        session.Status.Should().Be(SessionStatus.Active, "El estado de la sesión debe cambiar a Activo.");
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe actualizarse después de la activación.");
    }

    [Fact]
    public void Activate_CuandoSesionYaEstaActiva_NoDeberiaCambiarActividad()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        var initialActivity = session.LastActivityAt;

        // Act
        session.Activate();

        // Assert
        session.Status.Should().Be(SessionStatus.Active, "El estado de la sesión debe permanecer Activo.");
        session.LastActivityAt.Should().Be(initialActivity, "LastActivityAt no debe cambiar si la sesión ya está activa.");
    }

    [Fact]
    public void Expire_DeberiaEstablecerEstadoAExpiradoYActualizarActividad()
    {
        // Arrange
        var session = CreateValidSession();
        var initialActivity = session.LastActivityAt;

        // Act
        session.Expire();

        // Assert
        session.Status.Should().Be(SessionStatus.Expired, "El estado de la sesión debe cambiar a Expirada.");
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe actualizarse después de la expiración.");
    }

    [Fact]
    public void Revoke_DeberiaEstablecerEstadoARevocadoYActualizarActividad()
    {
        // Arrange
        var session = CreateValidSession();
        var initialActivity = session.LastActivityAt;

        // Act
        session.Revoke();

        // Assert
        session.Status.Should().Be(SessionStatus.Revoked, "El estado de la sesión debe cambiar a Revocada.");
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe actualizarse después de la revocación.");
    }

    [Fact]
    public void Invalidate_DeberiaEstablecerEstadoAInvalidadoYActualizarActividad()
    {
        // Arrange
        var session = CreateValidSession();
        var initialActivity = session.LastActivityAt;
        var reason = "Security violation";

        // Act
        session.Invalidate(reason);

        // Assert
        session.Status.Should().Be(SessionStatus.Invalidated, "El estado de la sesión debe cambiar a Invalidada.");
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe actualizarse después de la invalidación.");
        session.InvalidationReason.Should().Be(reason, "La razón de invalidación debe ser establecida.");
    }

    [Fact]
    public void MarkAsSuspicious_DeberiaEstablecerEstadoASuspendidoAumentarRiskScoreYActualizarActividad()
    {
        // Arrange
        var session = CreateValidSession();
        var initialActivity = session.LastActivityAt;
        var initialRiskScore = session.RiskScore;

        // Act
        session.MarkAsSuspicious("Abnormal activity");

        // Assert
        session.Status.Should().Be(SessionStatus.Suspended, "El estado de la sesión debe cambiar a Suspendida.");
        session.RiskScore.Should().BeGreaterThan(initialRiskScore, "El RiskScore debe aumentar.");
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe actualizarse.");
        session.SuspensionReason.Should().NotBeNullOrEmpty("La razón de suspensión debe ser establecida.");
    }

    [Fact]
    public void UpdateLastActivity_DeberiaActualizarLastActivityAt()
    {
        // Arrange
        var session = CreateValidSession();
        var initialActivity = session.LastActivityAt;

        // Act
        session.UpdateLastActivity();

        // Assert
        session.LastActivityAt.Should().BeAfter(initialActivity, "LastActivityAt debe ser posterior a la actividad inicial.");
    }

    [Fact]
    public void UpdateLocation_DeberiaActualizarUbicacionYReiniciarDispositivoConocido()
    {
        // Arrange
        var session = CreateValidSession();
        var newLocation = CreateValidLocationInfo("192.168.1.2");
        session.IsKnownDevice = true; // Simular que el dispositivo era conocido

        // Act
        session.UpdateLocation(newLocation);

        // Assert
        session.LocationInfo.Should().Be(newLocation, "La información de ubicación debe ser actualizada.");
        session.IsKnownDevice.Should().BeFalse("IsKnownDevice debe ser falso después de un cambio de ubicación.");
        session.LastIpAddress.Should().Be(newLocation.IpAddress, "LastIpAddress debe ser actualizada a la nueva IP.");
    }

    [Fact]
    public void ExtendExpiration_CuandoSesionEstaActiva_DeberiaExtenderAmbosTiemposDeExpiracion()
    {
        // Arrange
        var session = CreateValidSession();
        var initialExpiresAt = session.ExpiresAt;
        var initialRefreshTokenExpiresAt = session.RefreshTokenExpiresAt;

        // Act
        session.ExtendExpiration();

        // Assert
        session.ExpiresAt.Should().BeAfter(initialExpiresAt, "ExpiresAt debe extenderse.");
        session.RefreshTokenExpiresAt.Should().BeAfter(initialRefreshTokenExpiresAt, "RefreshTokenExpiresAt debe extenderse.");
    }

    [Fact]
    public void ExtendExpiration_CuandoSesionNoEstaActiva_NoDeberiaExtenderExpiracion()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Expired);
        var initialExpiresAt = session.ExpiresAt;
        var initialRefreshTokenExpiresAt = session.RefreshTokenExpiresAt;

        // Act
        session.ExtendExpiration();

        // Assert
        session.ExpiresAt.Should().Be(initialExpiresAt, "ExpiresAt no debe extenderse si la sesión no está activa.");
        session.RefreshTokenExpiresAt.Should().Be(initialRefreshTokenExpiresAt, "RefreshTokenExpiresAt no debe extenderse si la sesión no está activa.");
    }

    #endregion

    #region Pruebas de propiedades y asignación

    [Fact]
    public void SessionProperties_DeberianSerAsignadasCorrectamente()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionToken = "testSessionToken";
        var refreshToken = "testRefreshToken";
        var status = SessionStatus.Active;
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        var lastActivityAt = DateTime.UtcNow;
        var deviceInfo = CreateValidDeviceInfo();
        var locationInfo = CreateValidLocationInfo();
        var isKnownDevice = true;
        var loginMethod = LoginMethod.Password;
        var mfaVerified = true;
        var twoFactorRequired = false;
        var riskScore = 50;
        var initialIpAddress = "192.168.1.1";
        var lastIpAddress = "192.168.1.2";
        var invalidationReason = "Test Invalidation";
        var suspensionReason = "Test Suspension";
        var userAgent = "TestUserAgent";

        // Act
        var session = new Session
        {
            Id = sessionId,
            UserId = userId,
            SessionToken = sessionToken,
            RefreshToken = refreshToken,
            Status = status,
            ExpiresAt = expiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            LastActivityAt = lastActivityAt,
            DeviceInfo = deviceInfo,
            LocationInfo = locationInfo,
            IsKnownDevice = isKnownDevice,
            LoginMethod = loginMethod,
            MfaVerified = mfaVerified,
            TwoFactorRequired = twoFactorRequired,
            RiskScore = riskScore,
            InitialIpAddress = initialIpAddress,
            LastIpAddress = lastIpAddress,
            InvalidationReason = invalidationReason,
            SuspensionReason = suspensionReason,
            UserAgent = userAgent
        };

        // Assert
        session.Id.Should().Be(sessionId, "El Id de la sesión debe ser el asignado.");
        session.UserId.Should().Be(userId, "El UserId de la sesión debe ser el asignado.");
        session.SessionToken.Should().Be(sessionToken, "El SessionToken debe ser el asignado.");
        session.RefreshToken.Should().Be(refreshToken, "El RefreshToken debe ser el asignado.");
        session.Status.Should().Be(status, "El Status de la sesión debe ser el asignado.");
        session.ExpiresAt.Should().Be(expiresAt, "ExpiresAt debe ser la fecha asignada.");
        session.RefreshTokenExpiresAt.Should().Be(refreshTokenExpiresAt, "RefreshTokenExpiresAt debe ser la fecha asignada.");
        session.LastActivityAt.Should().Be(lastActivityAt, "LastActivityAt debe ser la fecha asignada.");
        session.DeviceInfo.Should().Be(deviceInfo, "DeviceInfo debe ser el asignado.");
        session.LocationInfo.Should().Be(locationInfo, "LocationInfo debe ser el asignado.");
        session.IsKnownDevice.Should().Be(isKnownDevice, "IsKnownDevice debe ser el asignado.");
        session.LoginMethod.Should().Be(loginMethod, "LoginMethod debe ser el asignado.");
        session.MfaVerified.Should().Be(mfaVerified, "MfaVerified debe ser el asignado.");
        session.TwoFactorRequired.Should().Be(twoFactorRequired, "TwoFactorRequired debe ser el asignado.");
        session.RiskScore.Should().Be(riskScore, "RiskScore debe ser el asignado.");
        session.InitialIpAddress.Should().Be(initialIpAddress, "InitialIpAddress debe ser el asignado.");
        session.LastIpAddress.Should().Be(lastIpAddress, "LastIpAddress debe ser el asignado.");
        session.InvalidationReason.Should().Be(invalidationReason, "InvalidationReason debe ser el asignado.");
        session.SuspensionReason.Should().Be(suspensionReason, "SuspensionReason debe ser el asignado.");
        session.UserAgent.Should().Be(userAgent, "UserAgent debe ser el asignado.");
    }

    #endregion

    #region Pruebas de lógica de negocio

    [Fact]
    public void IsActive_CuandoSesionEstaActivaYNoExpirada_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.ExpiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var isActive = session.IsActive();

        // Assert
        isActive.Should().BeTrue("La sesión debe considerarse activa si su estado es Activo y no ha expirado.");
    }

    [Fact]
    public void IsActive_CuandoEstadoDeSesionNoEsActivo_DeberiaRetornarFalse()
    {
        // Arrange
        var sessionSuspended = CreateValidSession(status: SessionStatus.Suspended);
        var sessionExpired = CreateValidSession(status: SessionStatus.Expired);
        var sessionRevoked = CreateValidSession(status: SessionStatus.Revoked);
        var sessionInvalidated = CreateValidSession(status: SessionStatus.Invalidated);

        // Act
        var isActiveSuspended = sessionSuspended.IsActive();
        var isActiveExpired = sessionExpired.IsActive();
        var isActiveRevoked = sessionRevoked.IsActive();
        var isActiveInvalidated = sessionInvalidated.IsActive();

        // Assert
        isActiveSuspended.Should().BeFalse("Una sesión suspendida no debe ser activa.");
        isActiveExpired.Should().BeFalse("Una sesión expirada no debe ser activa.");
        isActiveRevoked.Should().BeFalse("Una sesión revocada no debe ser activa.");
        isActiveInvalidated.Should().BeFalse("Una sesión invalidada no debe ser activa.");
    }

    [Fact]
    public void IsActive_CuandoSesionEstaExpirada_DeberiaRetornarFalse()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.ExpiresAt = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)); // Set expiration in the past

        // Act
        var isActive = session.IsActive();

        // Assert
        isActive.Should().BeFalse("Una sesión con ExpiresAt en el pasado no debe ser activa.");
    }

    [Fact]
    public void IsExpired_CuandoEstadoDeSesionEsExpirado_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Expired);

        // Act
        var isExpired = session.IsExpired();

        // Assert
        isExpired.Should().BeTrue("IsExpired debe ser verdadero si el estado de la sesión es Expirado.");
    }

    [Fact]
    public void IsExpired_CuandoTiempoDeExpiracionHaPasado_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.ExpiresAt = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)); // Expiration in the past

        // Act
        var isExpired = session.IsExpired();

        // Assert
        isExpired.Should().BeTrue("IsExpired debe ser verdadero si ExpiresAt es en el pasado, incluso si el estado es activo.");
    }

    [Fact]
    public void IsExpired_CuandoSesionEstaActivaYNoExpirada_DeberiaRetornarFalse()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(1); // Expiration in the future

        // Act
        var isExpired = session.IsExpired();

        // Assert
        isExpired.Should().BeFalse("IsExpired debe ser falso si ExpiresAt es en el futuro y la sesión está activa.");
    }

    [Fact]
    public void CanBeRefreshed_CuandoSesionEstaActivaYRefreshTokenNoExpirado_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1); // Refresh token in future

        // Act
        var canBeRefreshed = session.CanBeRefreshed();

        // Assert
        canBeRefreshed.Should().BeTrue("La sesión debe poder ser refrescada si está activa y el RefreshToken no ha expirado.");
    }

    [Fact]
    public void CanBeRefreshed_CuandoSesionNoEstaActiva_DeberiaRetornarFalse()
    {
        // Arrange
        var sessionSuspended = CreateValidSession(status: SessionStatus.Suspended);
        var sessionExpired = CreateValidSession(status: SessionStatus.Expired);
        var sessionRevoked = CreateValidSession(status: SessionStatus.Revoked);
        var sessionInvalidated = CreateValidSession(status: SessionStatus.Invalidated);

        // Act
        var canBeRefreshedSuspended = sessionSuspended.CanBeRefreshed();
        var canBeRefreshedExpired = sessionExpired.CanBeRefreshed();
        var canBeRefreshedRevoked = sessionRevoked.CanBeRefreshed();
        var canBeRefreshedInvalidated = sessionInvalidated.CanBeRefreshed();

        // Assert
        canBeRefreshedSuspended.Should().BeFalse("Una sesión suspendida no debe poder ser refrescada.");
        canBeRefreshedExpired.Should().BeFalse("Una sesión expirada no debe poder ser refrescada.");
        canBeRefreshedRevoked.Should().BeFalse("Una sesión revocada no debe poder ser refrescada.");
        canBeRefreshedInvalidated.Should().BeFalse("Una sesión invalidada no debe poder ser refrescada.");
    }

    [Fact]
    public void CanBeRefreshed_CuandoRefreshTokenEstaExpirado_DeberiaRetornarFalse()
    {
        // Arrange
        var session = CreateValidSession(status: SessionStatus.Active);
        session.RefreshTokenExpiresAt = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)); // Refresh token in past

        // Act
        var canBeRefreshed = session.CanBeRefreshed();

        // Assert
        canBeRefreshed.Should().BeFalse("La sesión no debe poder ser refrescada si el RefreshToken ha expirado.");
    }

    [Fact]
    public void ShouldRequireMfa_CuandoMfaNoVerificado_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(mfaVerified: false, twoFactorRequired: true);

        // Act
        var requiresMfa = session.ShouldRequireMfa();

        // Assert
        requiresMfa.Should().BeTrue("Debe requerir MFA si no ha sido verificado y two-factor está requerido.");
    }

    [Fact]
    public void ShouldRequireMfa_CuandoTwoFactorRequeridoYRiskScoreAlto_DeberiaRetornarTrue()
    {
        // Arrange
        var session = CreateValidSession(mfaVerified: false, twoFactorRequired: false, riskScore: 70); // MFA not verified, but high risk
        var user = CreateValidUser();
        session.User = user;

        // Act
        var requiresMfa = session.ShouldRequireMfa();

        // Assert
        requiresMfa.Should().BeTrue("Debe requerir MFA si hay un score de riesgo alto, incluso si two-factor no está explícitamente requerido.");
    }

    [Fact]
    public void ShouldRequireMfa_CuandoMfaVerificadoYRiskScoreBajo_DeberiaRetornarFalse()
    {
        // Arrange
        var session = CreateValidSession(mfaVerified: true, twoFactorRequired: true, riskScore: 10);
        var user = CreateValidUser();
        session.User = user;

        // Act
        var requiresMfa = session.ShouldRequireMfa();

        // Assert
        requiresMfa.Should().BeFalse("No debe requerir MFA si ya fue verificado y el score de riesgo es bajo.");
    }

    [Fact]
    public void ShouldRequireMfa_CuandoMfaVerificadoYTwoFactorNoRequerido_DeberiaRetornarFalse()
    {
        // Arrange
        var session = CreateValidSession(mfaVerified: true, twoFactorRequired: false);
        var user = CreateValidUser();
        session.User = user;

        // Act
        var requiresMfa = session.ShouldRequireMfa();

        // Assert
        requiresMfa.Should().BeFalse("No debe requerir MFA si ya fue verificado y two-factor no está requerido.");
    }

    [Fact]
    public void GenerateNewRefreshToken_DeberiaGenerarNuevoTokenYEstablecerExpiracion()
    {
        // Arrange
        var session = CreateValidSession();
        var oldRefreshToken = session.RefreshToken;
        var oldRefreshTokenExpiresAt = session.RefreshTokenExpiresAt;

        // Act
        session.GenerateNewRefreshToken();

        // Assert
        session.RefreshToken.Should().NotBe(oldRefreshToken, "Se debe generar un nuevo RefreshToken.");
        session.RefreshToken.Should().NotBeNullOrEmpty("El nuevo RefreshToken no debe ser nulo o vacío.");
        session.RefreshTokenExpiresAt.Should().BeAfter(oldRefreshTokenExpiresAt, "La nueva fecha de expiración del RefreshToken debe ser posterior a la anterior.");
        session.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow, "La nueva fecha de expiración del RefreshToken debe ser en el futuro.");
    }

    [Fact]
    public void RevokeRefreshToken_DeberiaLimpiarTokenYEstablecerExpiracionAPasado()
    {
        // Arrange
        var session = CreateValidSession();

        // Act
        session.RevokeRefreshToken();

        // Assert
        session.RefreshToken.Should().BeNullOrEmpty("El RefreshToken debe ser nulo o vacío después de la revocación.");
        session.RefreshTokenExpiresAt.Should().BeBefore(DateTime.UtcNow, "La fecha de expiración del RefreshToken debe ser en el pasado después de la revocación.");
    }

    #endregion

    #region Pruebas de escenarios de ciclo de vida

    [Fact]
    public void SessionLifecycle_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var user = CreateValidUser();
        var device = CreateValidDeviceInfo();
        var location = CreateValidLocationInfo();
        var session = Session.CreateNewSession(user, device, location, "password");

        // Assert - Estado inicial
        session.IsActive().Should().BeTrue("La sesión debe estar activa al ser creada.");
        session.CanBeRefreshed().Should().BeTrue("La sesión debe poder ser refrescada al ser creada.");

        // Act - Simular actividad
        Thread.Sleep(100); // Simular tiempo transcurrido
        session.UpdateLastActivity();
        session.LastActivityAt.Should().BeAfter(session.CreatedAt, "LastActivityAt debe actualizarse con la actividad.");

        // Act - Extender expiración
        var initialExpiresAt = session.ExpiresAt;
        session.ExtendExpiration();
        session.ExpiresAt.Should().BeAfter(initialExpiresAt, "ExpiresAt debe extenderse.");

        // Act - Simular refresco de token
        var oldRefreshToken = session.RefreshToken;
        session.GenerateNewRefreshToken();
        session.RefreshToken.Should().NotBe(oldRefreshToken, "Se debe generar un nuevo RefreshToken.");

        // Act - Revocar sesión
        session.Revoke();
        session.IsActive().Should().BeFalse("La sesión no debe estar activa después de ser revocada.");
        session.Status.Should().Be(SessionStatus.Revoked, "El estado debe ser Revoked después de la revocación.");

        // Act - Intentar activar sesión revocada (no debe cambiar)
        session.Activate();
        session.Status.Should().Be(SessionStatus.Revoked, "Una sesión revocada no debe poder ser activada.");
    }

    [Fact]
    public void TokenManagement_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var session = CreateValidSession();
        var initialSessionToken = session.SessionToken;
        var initialRefreshToken = session.RefreshToken;

        // Act & Assert: Generar nuevo refresh token
        session.GenerateNewRefreshToken();
        session.RefreshToken.Should().NotBe(initialRefreshToken, "El RefreshToken debe cambiar después de generar uno nuevo.");
        session.RefreshToken.Should().NotBeNullOrEmpty("El nuevo RefreshToken no debe ser nulo o vacío.");

        // Act & Assert: Revocar refresh token
        session.RevokeRefreshToken();
        session.RefreshToken.Should().BeNullOrEmpty("El RefreshToken debe ser nulo o vacío después de la revocación.");
        session.RefreshTokenExpiresAt.Should().BeBefore(DateTime.UtcNow, "La fecha de expiración del RefreshToken debe ser en el pasado después de la revocación.");

        // Act & Assert: Revocar sesión
        session.Revoke();
        session.Status.Should().Be(SessionStatus.Revoked, "El estado de la sesión debe ser Revoked.");
    }

    [Fact]
    public void LocationUpdate_DeberiaAfectarReconocimientoDeDispositivo()
    {
        // Arrange
        var session = CreateValidSession();
        session.IsKnownDevice = true; // Marcar como conocido inicialmente
        var newLocation = CreateValidLocationInfo("10.0.0.1");

        // Act
        session.UpdateLocation(newLocation);

        // Assert
        session.IsKnownDevice.Should().BeFalse("IsKnownDevice debe ser falso después de un cambio significativo de ubicación.");
        session.LocationInfo.IpAddress.Should().Be(newLocation.IpAddress, "La IP debe actualizarse a la nueva ubicación.");
    }

    [Fact]
    public void MfaRequirement_DeberiaConsiderarMultiplesFactores()
    {
        // Arrange
        var user = CreateValidUser();
        // Escenario 1: MFA no verificado, 2FA requerido por el usuario, riesgo bajo -> requiere MFA
        var session1 = CreateValidSession(user: user, mfaVerified: false, twoFactorRequired: true, riskScore: 10);
        session1.User = user;
        session1.ShouldRequireMfa().Should().BeTrue("Sesión 1: Debe requerir MFA si 2FA está activado y no verificado.");

        // Escenario 2: MFA verificado, 2FA no requerido por el usuario, riesgo bajo -> no MFA
        var session2 = CreateValidSession(user: user, mfaVerified: true, twoFactorRequired: false, riskScore: 10);
        session2.User = user;
        session2.ShouldRequireMfa().Should().BeFalse("Sesión 2: No debe requerir MFA si ya fue verificado y no es requerido.");

        // Escenario 3: MFA no verificado, 2FA no requerido por el usuario, riesgo alto -> requiere MFA
        var session3 = CreateValidSession(user: user, mfaVerified: false, twoFactorRequired: false, riskScore: 80);
        session3.User = user;
        session3.ShouldRequireMfa().Should().BeTrue("Sesión 3: Debe requerir MFA si el score de riesgo es alto.");

        // Escenario 4: MFA verificado, 2FA requerido por el usuario, riesgo alto -> requiere MFA (riesgo anula verificación)
        var session4 = CreateValidSession(user: user, mfaVerified: true, twoFactorRequired: true, riskScore: 90);
        session4.User = user;
        session4.ShouldRequireMfa().Should().BeTrue("Sesión 4: Debe requerir MFA si el score de riesgo es extremadamente alto.");
    }
    #endregion
}