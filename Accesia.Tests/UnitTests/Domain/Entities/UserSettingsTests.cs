using System.ComponentModel.DataAnnotations;

namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class UserSettingsTests
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

    private static UserSettings CreateValidUserSettings(Guid? userId = null)
    {
        return new UserSettings(userId ?? Guid.NewGuid());
    }

    #endregion

    #region Pruebas del constructor

    [Fact]
    public void UserSettings_ConUserIdValido_DeberiaCrearConValoresPorDefecto()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var userSettings = new UserSettings(userId);

        // Assert
        userSettings.Should().NotBeNull("La configuración de usuario no debe ser nula al crearse.");
        userSettings.UserId.Should().Be(userId, "El UserId de la configuración de usuario debe coincidir.");

        // Configuración de notificaciones por defecto
        userSettings.EmailNotificationsEnabled.Should().BeTrue("Las notificaciones por email deben estar habilitadas por defecto.");
        userSettings.SmsNotificationsEnabled.Should().BeFalse("Las notificaciones por SMS deben estar deshabilitadas por defecto.");
        userSettings.PushNotificationsEnabled.Should().BeTrue("Las notificaciones push deben estar habilitadas por defecto.");
        userSettings.InAppNotificationsEnabled.Should().BeTrue("Las notificaciones en la aplicación deben estar habilitadas por defecto.");

        // Configuración de notificaciones específicas por defecto
        userSettings.SecurityAlertsEnabled.Should().BeTrue("Las alertas de seguridad deben estar habilitadas por defecto.");
        userSettings.LoginActivityNotificationsEnabled.Should().BeTrue("Las notificaciones de actividad de inicio de sesión deben estar habilitadas por defecto.");
        userSettings.PasswordChangeNotificationsEnabled.Should().BeTrue("Las notificaciones de cambio de contraseña deben estar habilitadas por defecto.");
        userSettings.AccountUpdateNotificationsEnabled.Should().BeTrue("Las notificaciones de actualización de cuenta deben estar habilitadas por defecto.");
        userSettings.SystemAnnouncementsEnabled.Should().BeTrue("Los anuncios del sistema deben estar habilitados por defecto.");
        userSettings.DeviceActivityNotificationsEnabled.Should().BeTrue("Las notificaciones de actividad del dispositivo deben estar habilitadas por defecto.");

        // Configuración de privacidad por defecto
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Private, "La visibilidad del perfil debe ser Privada por defecto.");
        userSettings.ShowLastLoginTime.Should().BeFalse("Mostrar la última hora de inicio de sesión debe ser falso por defecto.");
        userSettings.ShowOnlineStatus.Should().BeFalse("Mostrar el estado en línea debe ser falso por defecto.");
        userSettings.AllowDataCollection.Should().BeFalse("La recolección de datos debe estar deshabilitada por defecto.");
        userSettings.AllowMarketingEmails.Should().BeFalse("Los correos de marketing deben estar deshabilitados por defecto.");

        // Configuración de localización por defecto
        userSettings.PreferredLanguage.Should().Be("es", "El idioma preferido debe ser 'es' por defecto.");
        userSettings.TimeZone.Should().Be("America/Bogota", "La zona horaria debe ser 'America/Bogota' por defecto.");
        userSettings.DateFormat.Should().Be("dd/MM/yyyy", "El formato de fecha debe ser 'dd/MM/yyyy' por defecto.");
        userSettings.TimeFormat.Should().Be("24h", "El formato de hora debe ser '24h' por defecto.");

        // Configuración de seguridad por defecto
        userSettings.TwoFactorAuthEnabled.Should().BeFalse("La autenticación de dos factores debe estar deshabilitada por defecto.");
        userSettings.RequirePasswordChangeOn2FADisable.Should().BeTrue("Se debe requerir cambio de contraseña al deshabilitar 2FA por defecto.");
        userSettings.LogoutOnPasswordChange.Should().BeTrue("Cerrar sesión al cambiar contraseña debe estar habilitado por defecto.");
        userSettings.SessionTimeoutMinutes.Should().Be(60, "El tiempo de espera de la sesión debe ser de 60 minutos por defecto.");
    }

    [Fact]
    public void UserSettings_ConUserIdVacio_DeberiaCrearIgualmente()
    {
        // Arrange
        var userId = Guid.Empty;

        // Act
        var userSettings = new UserSettings(userId);

        // Assert
        userSettings.Should().NotBeNull("La configuración de usuario no debe ser nula incluso con UserId vacío.");
        userSettings.UserId.Should().Be(userId, "El UserId debe coincidir incluso si está vacío.");
    }

    #endregion

    #region Pruebas de Métodos de Fábrica

    [Fact]
    public void CreateDefault_ConUserIdValido_DeberiaCrearUserSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var userSettings = UserSettings.CreateDefault(userId);

        // Assert
        userSettings.Should().NotBeNull("La configuración de usuario creada por el método de fábrica no debe ser nula.");
        userSettings.UserId.Should().Be(userId, "El UserId de la configuración de usuario creada por el método de fábrica debe coincidir.");
        userSettings.PreferredLanguage.Should().Be("es", "El idioma preferido debe ser 'es' por defecto.");
        userSettings.SessionTimeoutMinutes.Should().Be(60, "El tiempo de espera de la sesión debe ser de 60 minutos por defecto.");
    }

    [Fact]
    public void CreateDefault_DeberiaComportarseIdenticamenteAlConstructor()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var settingsFromConstructor = new UserSettings(userId);
        var settingsFromFactory = UserSettings.CreateDefault(userId);

        // Assert
        settingsFromConstructor.UserId.Should().Be(settingsFromFactory.UserId, "El UserId debe ser idéntico entre el constructor y el método de fábrica.");
        settingsFromConstructor.EmailNotificationsEnabled.Should().Be(settingsFromFactory.EmailNotificationsEnabled, "Las notificaciones por email deben ser idénticas.");
        settingsFromConstructor.PreferredLanguage.Should().Be(settingsFromFactory.PreferredLanguage, "El idioma preferido debe ser idéntico.");
        settingsFromConstructor.SessionTimeoutMinutes.Should().Be(settingsFromFactory.SessionTimeoutMinutes, "El tiempo de espera de la sesión debe ser idéntico.");
    }

    #endregion

    #region Pruebas de Gestión de Tipos de Notificación

    [Theory]
    [InlineData(NotificationType.SecurityAlert, true)]
    [InlineData(NotificationType.LoginActivity, false)]
    [InlineData(NotificationType.PasswordChange, true)]
    [InlineData(NotificationType.AccountUpdate, false)]
    [InlineData(NotificationType.SystemAnnouncement, true)]
    [InlineData(NotificationType.DeviceActivity, false)]
    public void EnableNotificationType_DeberiaActualizarPropiedadCorrecta(NotificationType type, bool enabled)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.EnableNotificationType(type, enabled);

        // Assert
        switch (type)
        {
            case NotificationType.SecurityAlert:
                userSettings.SecurityAlertsEnabled.Should().Be(enabled, $"SecurityAlertsEnabled debe ser {enabled} para {type}.");
                break;
            case NotificationType.LoginActivity:
                userSettings.LoginActivityNotificationsEnabled.Should().Be(enabled, $"LoginActivityNotificationsEnabled debe ser {enabled} para {type}.");
                break;
            case NotificationType.PasswordChange:
                userSettings.PasswordChangeNotificationsEnabled.Should().Be(enabled, $"PasswordChangeNotificationsEnabled debe ser {enabled} para {type}.");
                break;
            case NotificationType.AccountUpdate:
                userSettings.AccountUpdateNotificationsEnabled.Should().Be(enabled, $"AccountUpdateNotificationsEnabled debe ser {enabled} para {type}.");
                break;
            case NotificationType.SystemAnnouncement:
                userSettings.SystemAnnouncementsEnabled.Should().Be(enabled, $"SystemAnnouncementsEnabled debe ser {enabled} para {type}.");
                break;
            case NotificationType.DeviceActivity:
                userSettings.DeviceActivityNotificationsEnabled.Should().Be(enabled, $"DeviceActivityNotificationsEnabled debe ser {enabled} para {type}.");
                break;
        }
    }

    [Fact]
    public void EnableNotificationType_SinParametroEnabled_DeberiaSerTruePorDefecto()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        userSettings.SecurityAlertsEnabled = false; // Set to false initially

        // Act
        userSettings.EnableNotificationType(NotificationType.SecurityAlert);

        // Assert
        userSettings.SecurityAlertsEnabled.Should().BeTrue("Al habilitar un tipo de notificación sin parámetro, debe establecerse a verdadero.");
    }

    [Fact]
    public void EnableNotificationType_TodosLosTiposImplementados_DeberianActualizarseCorrectamente()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        // Solo testear tipos de notificación que están implementados en UserSettings
        var implementedTypes = new[]
        {
            NotificationType.SecurityAlert,
            NotificationType.LoginActivity,
            NotificationType.PasswordChange,
            NotificationType.AccountUpdate,
            NotificationType.SystemAnnouncement,
            NotificationType.DeviceActivity
        };

        // Act & Assert
        foreach (var type in implementedTypes)
        {
            userSettings.EnableNotificationType(type, false);
            userSettings.EnableNotificationType(type, true);

            // Verificar que la propiedad específica sea verdadera
            switch (type)
            {
                case NotificationType.SecurityAlert:
                    userSettings.SecurityAlertsEnabled.Should().BeTrue($"SecurityAlertsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
                case NotificationType.LoginActivity:
                    userSettings.LoginActivityNotificationsEnabled.Should().BeTrue($"LoginActivityNotificationsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
                case NotificationType.PasswordChange:
                    userSettings.PasswordChangeNotificationsEnabled.Should().BeTrue($"PasswordChangeNotificationsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
                case NotificationType.AccountUpdate:
                    userSettings.AccountUpdateNotificationsEnabled.Should().BeTrue($"AccountUpdateNotificationsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
                case NotificationType.SystemAnnouncement:
                    userSettings.SystemAnnouncementsEnabled.Should().BeTrue($"SystemAnnouncementsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
                case NotificationType.DeviceActivity:
                    userSettings.DeviceActivityNotificationsEnabled.Should().BeTrue($"DeviceActivityNotificationsEnabled debe ser verdadero después de habilitarlo para {type}.");
                    break;
            }
        }
    }

    #endregion

    #region Pruebas de Gestión de Canales de Notificación

    [Theory]
    [InlineData(NotificationChannel.Email, true)]
    [InlineData(NotificationChannel.Sms, false)]
    [InlineData(NotificationChannel.Push, true)]
    [InlineData(NotificationChannel.InApp, false)]
    public void EnableNotificationChannel_DeberiaActualizarPropiedadCorrecta(NotificationChannel channel, bool enabled)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.EnableNotificationChannel(channel, enabled);

        // Assert
        switch (channel)
        {
            case NotificationChannel.Email:
                userSettings.EmailNotificationsEnabled.Should().Be(enabled, $"EmailNotificationsEnabled debe ser {enabled} para el canal {channel}.");
                break;
            case NotificationChannel.Sms:
                userSettings.SmsNotificationsEnabled.Should().Be(enabled, $"SmsNotificationsEnabled debe ser {enabled} para el canal {channel}.");
                break;
            case NotificationChannel.Push:
                userSettings.PushNotificationsEnabled.Should().Be(enabled, $"PushNotificationsEnabled debe ser {enabled} para el canal {channel}.");
                break;
            case NotificationChannel.InApp:
                userSettings.InAppNotificationsEnabled.Should().Be(enabled, $"InAppNotificationsEnabled debe ser {enabled} para el canal {channel}.");
                break;
        }
    }

    [Fact]
    public void EnableNotificationChannel_SinParametroEnabled_DeberiaSerTruePorDefecto()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        userSettings.EmailNotificationsEnabled = false; // Establecer a false inicialmente

        // Act
        userSettings.EnableNotificationChannel(NotificationChannel.Email);

        // Assert
        userSettings.EmailNotificationsEnabled.Should().BeTrue("Al habilitar un canal de notificación sin parámetro, debe establecerse a verdadero.");
    }

    [Fact]
    public void EnableNotificationChannel_TodosLosCanales_DeberianActualizarseCorrectamente()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var allChannels = new[]
        {
            NotificationChannel.Email,
            NotificationChannel.Sms,
            NotificationChannel.Push,
            NotificationChannel.InApp
        };

        // Act & Assert
        foreach (var channel in allChannels)
        {
            userSettings.EnableNotificationChannel(channel, false);
            userSettings.EnableNotificationChannel(channel, true);

            switch (channel)
            {
                case NotificationChannel.Email:
                    userSettings.EmailNotificationsEnabled.Should().BeTrue($"EmailNotificationsEnabled debe ser verdadero después de habilitarlo para {channel}.");
                    break;
                case NotificationChannel.Sms:
                    userSettings.SmsNotificationsEnabled.Should().BeTrue($"SmsNotificationsEnabled debe ser verdadero después de habilitarlo para {channel}.");
                    break;
                case NotificationChannel.Push:
                    userSettings.PushNotificationsEnabled.Should().BeTrue($"PushNotificationsEnabled debe ser verdadero después de habilitarlo para {channel}.");
                    break;
                case NotificationChannel.InApp:
                    userSettings.InAppNotificationsEnabled.Should().BeTrue($"InAppNotificationsEnabled debe ser verdadero después de habilitarlo para {channel}.");
                    break;
            }
        }
    }

    #endregion

    #region Pruebas de Combinación de Notificaciones

    [Theory]
    [InlineData(NotificationType.SecurityAlert, NotificationChannel.Email, true, true, true)]
    [InlineData(NotificationType.SecurityAlert, NotificationChannel.Email, true, false, false)]
    [InlineData(NotificationType.SecurityAlert, NotificationChannel.Email, false, true, false)]
    [InlineData(NotificationType.SecurityAlert, NotificationChannel.Email, false, false, false)]
    [InlineData(NotificationType.LoginActivity, NotificationChannel.Sms, true, true, true)]
    [InlineData(NotificationType.PasswordChange, NotificationChannel.Push, true, true, true)]
    [InlineData(NotificationType.AccountUpdate, NotificationChannel.InApp, true, true, true)]
    public void IsNotificationEnabled_DeberiaRetornarCombinacionCorrecta(
        NotificationType type, NotificationChannel channel, bool typeEnabled, bool channelEnabled, bool expectedResult)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        userSettings.EnableNotificationType(type, typeEnabled);
        userSettings.EnableNotificationChannel(channel, channelEnabled);

        // Act
        var isEnabled = userSettings.IsNotificationEnabled(type, channel);

        // Assert
        isEnabled.Should().Be(expectedResult, $"La combinación de tipo '{type}' ({typeEnabled}) y canal '{channel}' ({channelEnabled}) debe resultar en {expectedResult}.");
    }

    [Fact]
    public void IsNotificationEnabled_ConTodasLasCombinaciones_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var notificationTypes = Enum.GetValues<NotificationType>();
        var notificationChannels = Enum.GetValues<NotificationChannel>();

        // Act & Assert
        foreach (var type in notificationTypes)
        {
            foreach (var channel in notificationChannels)
            {
                // Reiniciar configuraciones para cada combinación para asegurar pruebas independientes
                userSettings = CreateValidUserSettings(userSettings.UserId);

                // Caso 1: Ambos tipo y canal habilitados
                userSettings.EnableNotificationType(type, true);
                userSettings.EnableNotificationChannel(channel, true);
                userSettings.IsNotificationEnabled(type, channel).Should().BeTrue($"Tipo {type} y Canal {channel} ambos habilitados deben resultar en verdadero.");

                // Caso 2: Tipo habilitado, canal deshabilitado
                userSettings = CreateValidUserSettings(userSettings.UserId);
                userSettings.EnableNotificationType(type, true);
                userSettings.EnableNotificationChannel(channel, false);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} habilitado y Canal {channel} deshabilitado deben resultar en falso.");

                // Caso 3: Tipo deshabilitado, canal habilitado
                userSettings = CreateValidUserSettings(userSettings.UserId);
                userSettings.EnableNotificationType(type, false);
                userSettings.EnableNotificationChannel(channel, true);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} deshabilitado y Canal {channel} habilitado deben resultar en falso.");

                // Caso 4: Ambos tipo y canal deshabilitados
                userSettings = CreateValidUserSettings(userSettings.UserId);
                userSettings.EnableNotificationType(type, false);
                userSettings.EnableNotificationChannel(channel, false);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} y Canal {channel} ambos deshabilitados deben resultar en falso.");
            }
        }
    }

    #endregion

    #region Pruebas de Configuración de Privacidad

    [Fact]
    public void UpdatePrivacySettings_DeberiaActualizarTodasLasPropiedades()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var newProfileVisibility = PrivacyLevel.Public;
        var newShowLastLoginTime = true;
        var newShowOnlineStatus = true;
        var newAllowDataCollection = true;
        var newAllowMarketingEmails = true;

        // Act
        userSettings.UpdatePrivacySettings(
            newProfileVisibility,
            newShowLastLoginTime,
            newShowOnlineStatus,
            newAllowDataCollection,
            newAllowMarketingEmails);

        // Assert
        userSettings.ProfileVisibility.Should().Be(newProfileVisibility, "La visibilidad del perfil debe ser actualizada.");
        userSettings.ShowLastLoginTime.Should().Be(newShowLastLoginTime, "Mostrar la última hora de inicio de sesión debe ser actualizada.");
        userSettings.ShowOnlineStatus.Should().Be(newShowOnlineStatus, "Mostrar el estado en línea debe ser actualizada.");
        userSettings.AllowDataCollection.Should().Be(newAllowDataCollection, "La recolección de datos debe ser actualizada.");
        userSettings.AllowMarketingEmails.Should().Be(newAllowMarketingEmails, "Los correos de marketing deben ser actualizada.");
    }

    [Theory]
    [InlineData(PrivacyLevel.Private, false, false, false, false)]
    [InlineData(PrivacyLevel.FriendsOnly, true, false, true, false)]
    [InlineData(PrivacyLevel.Public, true, true, true, true)]
    public void UpdatePrivacySettings_ConDiferentesCombinaciones_DeberiaActualizarCorrectamente(
        PrivacyLevel profileVisibility, bool showLastLoginTime, bool showOnlineStatus,
        bool allowDataCollection, bool allowMarketingEmails)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdatePrivacySettings(
            profileVisibility,
            showLastLoginTime,
            showOnlineStatus,
            allowDataCollection,
            allowMarketingEmails);

        // Assert
        userSettings.ProfileVisibility.Should().Be(profileVisibility, $"ProfileVisibility debe ser {profileVisibility}.");
        userSettings.ShowLastLoginTime.Should().Be(showLastLoginTime, $"ShowLastLoginTime debe ser {showLastLoginTime}.");
        userSettings.ShowOnlineStatus.Should().Be(showOnlineStatus, $"ShowOnlineStatus debe ser {showOnlineStatus}.");
        userSettings.AllowDataCollection.Should().Be(allowDataCollection, $"AllowDataCollection debe ser {allowDataCollection}.");
        userSettings.AllowMarketingEmails.Should().Be(allowMarketingEmails, $"AllowMarketingEmails debe ser {allowMarketingEmails}.");
    }

    #endregion

    #region Pruebas de Configuración de Localización

    [Fact]
    public void UpdateLocalizationSettings_ConTodosLosParametros_DeberiaActualizarTodo()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var newLanguage = "en";
        var newTimeZone = "America/New_York";
        var newDateFormat = "MM/dd/yyyy";
        var newTimeFormat = "12h";

        // Act
        userSettings.UpdateLocalizationSettings(newLanguage, newTimeZone, newDateFormat, newTimeFormat);

        // Assert
        userSettings.PreferredLanguage.Should().Be(newLanguage, "El idioma preferido debe ser actualizado.");
        userSettings.TimeZone.Should().Be(newTimeZone, "La zona horaria debe ser actualizada.");
        userSettings.DateFormat.Should().Be(newDateFormat, "El formato de fecha debe ser actualizado.");
        userSettings.TimeFormat.Should().Be(newTimeFormat, "El formato de hora debe ser actualizado.");
    }

    [Fact]
    public void UpdateLocalizationSettings_ConParametrosParciales_DeberiaActualizarSoloLosProporcionados()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var originalLanguage = userSettings.PreferredLanguage;
        var newTimeZone = "Europe/London";

        // Act
        userSettings.UpdateLocalizationSettings(timeZone: newTimeZone);

        // Assert
        userSettings.PreferredLanguage.Should().Be(originalLanguage, "El idioma preferido no debe cambiar si no se proporciona.");
        userSettings.TimeZone.Should().Be(newTimeZone, "La zona horaria debe ser actualizada.");
        userSettings.DateFormat.Should().NotBeNullOrEmpty("El formato de fecha no debe ser nulo o vacío."); // Default value
    }

    [Theory]
    [InlineData(null, "Should not update")]
    [InlineData("", "Should not update")]
    [InlineData("   ", "Should not update")]
    [InlineData("fr", "Should update")]
    public void UpdateLocalizationSettings_ConValoresNulosOVacios_NoDeberiaActualizar(string? language, string description)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var originalLanguage = userSettings.PreferredLanguage;

        // Act
        userSettings.UpdateLocalizationSettings(language: language);

        // Assert
        if (string.IsNullOrWhiteSpace(language))
        {
            userSettings.PreferredLanguage.Should().Be(originalLanguage, $"El idioma no debe actualizarse si se proporciona un valor nulo o vacío: {description}.");
        }
        else
        {
            userSettings.PreferredLanguage.Should().Be(language, $"El idioma debe actualizarse a '{language}': {description}.");
        }
    }

    [Theory]
    [InlineData("en", "UTC", "MM/dd/yyyy", "12h")]
    [InlineData("fr", "Europe/Paris", "dd/MM/yyyy", "24h")]
    [InlineData("de", "Europe/Berlin", "dd.MM.yyyy", "24h")]
    [InlineData("ja", "Asia/Tokyo", "yyyy/MM/dd", "24h")]
    public void UpdateLocalizationSettings_ConDiferentesLocalizaciones_DeberiaActualizarCorrectamente(
        string language, string timeZone, string dateFormat, string timeFormat)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateLocalizationSettings(language, timeZone, dateFormat, timeFormat);

        // Assert
        userSettings.PreferredLanguage.Should().Be(language, $"PreferredLanguage debe ser '{language}'.");
        userSettings.TimeZone.Should().Be(timeZone, $"TimeZone debe ser '{timeZone}'.");
        userSettings.DateFormat.Should().Be(dateFormat, $"DateFormat debe ser '{dateFormat}'.");
        userSettings.TimeFormat.Should().Be(timeFormat, $"TimeFormat debe ser '{timeFormat}'.");
    }

    #endregion

    #region Pruebas de Configuración de Seguridad

    [Fact]
    public void UpdateSecuritySettings_DeberiaActualizarTodasLasPropiedades()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var newTwoFactorAuthEnabled = true;
        var newRequirePasswordChangeOn2FADisable = false;
        var newLogoutOnPasswordChange = false;
        var newSessionTimeoutMinutes = 120;

        // Act
        userSettings.UpdateSecuritySettings(
            newTwoFactorAuthEnabled,
            newRequirePasswordChangeOn2FADisable,
            newLogoutOnPasswordChange,
            newSessionTimeoutMinutes);

        // Assert
        userSettings.TwoFactorAuthEnabled.Should().Be(newTwoFactorAuthEnabled, "TwoFactorAuthEnabled debe ser actualizado.");
        userSettings.RequirePasswordChangeOn2FADisable.Should().Be(newRequirePasswordChangeOn2FADisable, "RequirePasswordChangeOn2FADisable debe ser actualizado.");
        userSettings.LogoutOnPasswordChange.Should().Be(newLogoutOnPasswordChange, "LogoutOnPasswordChange debe ser actualizado.");
        userSettings.SessionTimeoutMinutes.Should().Be(newSessionTimeoutMinutes, "SessionTimeoutMinutes debe ser actualizado.");
    }

    [Theory]
    [InlineData(1, 5)] // Por debajo del mínimo
    [InlineData(5, 5)] // En el mínimo
    [InlineData(30, 30)] // Valor normal
    [InlineData(480, 480)] // En el máximo
    [InlineData(600, 480)] // Por encima del máximo
    [InlineData(0, 5)] // Cero debe convertirse en el mínimo
    [InlineData(-10, 5)] // Negativo debe convertirse en el mínimo
    public void UpdateSecuritySettings_SessionTimeout_DeberiaLimitarEntre5Y480Minutos(int input, int expected)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateSecuritySettings(sessionTimeoutMinutes: input);

        // Assert
        userSettings.SessionTimeoutMinutes.Should().Be(expected, $"SessionTimeoutMinutes con entrada {input} debe ser {expected} después de clamping.");
    }

    [Theory]
    [InlineData(true, false, true, 180)]
    [InlineData(false, true, false, 30)]
    [InlineData(true, true, true, 240)]
    [InlineData(false, false, false, 15)]
    public void UpdateSecuritySettings_ConDiferentesCombinaciones_DeberiaActualizarCorrectamente(
        bool twoFactorAuthEnabled, bool requirePasswordChangeOn2FADisable,
        bool logoutOnPasswordChange, int sessionTimeoutMinutes)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateSecuritySettings(
            twoFactorAuthEnabled,
            requirePasswordChangeOn2FADisable,
            logoutOnPasswordChange,
            sessionTimeoutMinutes);

        // Assert
        userSettings.TwoFactorAuthEnabled.Should().Be(twoFactorAuthEnabled, $"TwoFactorAuthEnabled debe ser {twoFactorAuthEnabled}.");
        userSettings.RequirePasswordChangeOn2FADisable.Should().Be(requirePasswordChangeOn2FADisable, $"RequirePasswordChangeOn2FADisable debe ser {requirePasswordChangeOn2FADisable}.");
        userSettings.LogoutOnPasswordChange.Should().Be(logoutOnPasswordChange, $"LogoutOnPasswordChange debe ser {logoutOnPasswordChange}.");
        userSettings.SessionTimeoutMinutes.Should().Be(sessionTimeoutMinutes, $"SessionTimeoutMinutes debe ser {sessionTimeoutMinutes}.");
    }

    #endregion

    #region Pruebas de Propiedades de Entidad y Navegación

    [Fact]
    public void UserId_DeberiaSerAsignable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UserId = userId;

        // Assert
        userSettings.UserId.Should().Be(userId, "UserId debe ser asignable y mantener el valor.");
    }

    [Fact]
    public void UserSettings_DeberiaInicializarPropiedadDeNavegacionUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSettings = new UserSettings(userId);

        // Act
        userSettings.User = CreateValidUser(); // Simular la carga de usuario relacionado de EF Core

        // Assert
        userSettings.User.Should().NotBeNull("La propiedad de navegación User debe ser inicializable.");
        userSettings.User.Email.Value.Should().Be("test@example.com", "El email del usuario de navegación debe coincidir.");
    }

    #endregion

    #region Pruebas de Entidad Auditable

    [Fact]
    public void UserSettings_DeberiaHeredarDeAuditableEntity()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act & Assert
        userSettings.Should().BeAssignableTo<AuditableEntity>("UserSettings debe heredar de AuditableEntity.");
    }

    [Fact]
    public void UserSettings_DeberiaTenerPropiedadesAuditables()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var lastModifiedAt = DateTime.UtcNow.AddMinutes(-1);
        userSettings.CreatedAt = createdAt;
        userSettings.LastModifiedAt = lastModifiedAt;

        // Assert
        userSettings.CreatedAt.Should().Be(createdAt, "CreatedAt debe ser asignable.");
        userSettings.LastModifiedAt.Should().Be(lastModifiedAt, "LastModifiedAt debe ser asignable.");
    }

    #endregion

    #region Pruebas de Atributos de Validación

    [Fact]
    public void UserSettings_DeberiaTenerAtributosMaxLengthCorrectos()
    {
        // Arrange
        var settings = new UserSettings(Guid.NewGuid());

        // Act & Assert
        typeof(UserSettings).GetProperty(nameof(UserSettings.PreferredLanguage))
            .Should().HaveAttribute<MaxLengthAttribute>()
            .And.Subject.Length.Should().Be(10, "PreferredLanguage debe tener MaxLength de 10.");
        typeof(UserSettings).GetProperty(nameof(UserSettings.TimeZone))
            .Should().HaveAttribute<MaxLengthAttribute>()
            .And.Subject.Length.Should().Be(50, "TimeZone debe tener MaxLength de 50.");
        typeof(UserSettings).GetProperty(nameof(UserSettings.DateFormat))
            .Should().HaveAttribute<MaxLengthAttribute>()
            .And.Subject.Length.Should().Be(20, "DateFormat debe tener MaxLength de 20.");
        typeof(UserSettings).GetProperty(nameof(UserSettings.TimeFormat))
            .Should().HaveAttribute<MaxLengthAttribute>()
            .And.Subject.Length.Should().Be(10, "TimeFormat debe tener MaxLength de 10.");
    }

    [Fact]
    public void UserSettings_DeberiaTenerAtributoRequired()
    {
        // Arrange
        var settings = new UserSettings(Guid.NewGuid());

        // Act & Assert
        typeof(UserSettings).GetProperty(nameof(UserSettings.UserId))
            .Should().HaveAttribute<RequiredAttribute>("UserId debe tener el atributo Required.");
    }

    #endregion

    #region Pruebas de Escenarios de Ciclo de Vida e Integración

    [Fact]
    public void UserSettingsLifecycle_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSettings = new UserSettings(userId);

        // Assert Estado inicial
        userSettings.EmailNotificationsEnabled.Should().BeTrue("EmailNotificationsEnabled debe ser verdadero por defecto.");
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Private, "ProfileVisibility debe ser Privada por defecto.");
        userSettings.PreferredLanguage.Should().Be("es", "PreferredLanguage debe ser 'es' por defecto.");
        userSettings.TwoFactorAuthEnabled.Should().BeFalse("TwoFactorAuthEnabled debe ser falso por defecto.");

        // Act - Actualizar varias configuraciones
        userSettings.EnableNotificationChannel(NotificationChannel.Sms, true);
        userSettings.EnableNotificationType(NotificationType.LoginActivity, true);
        userSettings.UpdatePrivacySettings(PrivacyLevel.Public, true, true, true, true);
        userSettings.UpdateLocalizationSettings("en", "America/New_York", "MM/dd/yyyy", "12h");
        userSettings.UpdateSecuritySettings(true, false, false, 90);

        // Assert Estado actualizado
        userSettings.SmsNotificationsEnabled.Should().BeTrue("SmsNotificationsEnabled debe ser verdadero después de la actualización.");
        userSettings.LoginActivityNotificationsEnabled.Should().BeTrue("LoginActivityNotificationsEnabled debe ser verdadero después de la actualización.");
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Public, "ProfileVisibility debe ser Public después de la actualización.");
        userSettings.ShowLastLoginTime.Should().BeTrue("ShowLastLoginTime debe ser verdadero después de la actualización.");
        userSettings.PreferredLanguage.Should().Be("en", "PreferredLanguage debe ser 'en' después de la actualización.");
        userSettings.TimeZone.Should().Be("America/New_York", "TimeZone debe ser 'America/New_York' después de la actualización.");
        userSettings.TwoFactorAuthEnabled.Should().BeTrue("TwoFactorAuthEnabled debe ser verdadero después de la actualización.");
        userSettings.SessionTimeoutMinutes.Should().Be(90, "SessionTimeoutMinutes debe ser 90 después de la actualización.");

        // Act - Revertir algunas configuraciones
        userSettings.EnableNotificationChannel(NotificationChannel.Sms, false);
        userSettings.UpdatePrivacySettings(PrivacyLevel.Private);

        // Assert Estado revertido
        userSettings.SmsNotificationsEnabled.Should().BeFalse("SmsNotificationsEnabled debe ser falso después de revertir.");
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Private, "ProfileVisibility debe ser Privada después de revertir.");
    }

    [Fact]
    public void MatrizDeNotificaciones_DeberiaFuncionarCorrectamenteParaTodasLasCombinaciones()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var types = Enum.GetValues<NotificationType>();
        var channels = Enum.GetValues<NotificationChannel>();

        foreach (var type in types)
        {
            foreach (var channel in channels)
            {
                // Test cuando ambos están habilitados
                userSettings.EnableNotificationType(type, true);
                userSettings.EnableNotificationChannel(channel, true);
                userSettings.IsNotificationEnabled(type, channel).Should().BeTrue($"Tipo {type} y Canal {channel} deben estar habilitados.");

                // Test cuando el tipo está deshabilitado
                userSettings.EnableNotificationType(type, false);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} deshabilitado, Canal {channel} habilitado debe estar deshabilitado.");

                // Test cuando el tipo está habilitado pero el canal está deshabilitado
                userSettings.EnableNotificationType(type, true);
                userSettings.EnableNotificationChannel(channel, false);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} habilitado, Canal {channel} deshabilitado debe estar deshabilitado.");

                // Test cuando ambos están deshabilitados
                userSettings.EnableNotificationType(type, false);
                userSettings.EnableNotificationChannel(channel, false);
                userSettings.IsNotificationEnabled(type, channel).Should().BeFalse($"Tipo {type} y Canal {channel} ambos deshabilitados deben estar deshabilitados.");
            }
        }
    }

    [Fact]
    public void MultiplesActualizacionesDeConfiguracion_DeberianMantenerConsistencia()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act - Primer conjunto de actualizaciones
        userSettings.UpdatePrivacySettings(PrivacyLevel.Public, true, true, true, true);
        userSettings.UpdateLocalizationSettings("fr", "Europe/Paris", "yyyy-MM-dd", "12h");
        userSettings.UpdateSecuritySettings(true, true, true, 180);

        // Assert - Primera verificación
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Public, "ProfileVisibility debe ser Public después de la primera actualización.");
        userSettings.PreferredLanguage.Should().Be("fr", "PreferredLanguage debe ser 'fr' después de la primera actualización.");
        userSettings.TwoFactorAuthEnabled.Should().BeTrue("TwoFactorAuthEnabled debe ser verdadero después de la primera actualización.");

        // Act - Segundo conjunto de actualizaciones (parcial)
        userSettings.UpdateLocalizationSettings(timeZone: "Asia/Tokyo");
        userSettings.UpdateSecuritySettings(sessionTimeoutMinutes: 60);

        // Assert - Segunda verificación
        userSettings.PreferredLanguage.Should().Be("fr", "PreferredLanguage no debe cambiar en la segunda actualización si no se especifica.");
        userSettings.TimeZone.Should().Be("Asia/Tokyo", "TimeZone debe ser 'Asia/Tokyo' después de la segunda actualización.");
        userSettings.SessionTimeoutMinutes.Should().Be(60, "SessionTimeoutMinutes debe ser 60 después de la segunda actualización.");

        // Act - Tercer conjunto de actualizaciones (revertir)
        userSettings.UpdatePrivacySettings(PrivacyLevel.Private);

        // Assert - Tercera verificación
        userSettings.ProfileVisibility.Should().Be(PrivacyLevel.Private, "ProfileVisibility debe ser Privada después de la tercera actualización.");
    }

    [Theory]
    [InlineData("es", "America/Bogota", "dd/MM/yyyy", "24h")] // Español/Colombia
    [InlineData("en", "America/New_York", "MM/dd/yyyy", "12h")] // Inglés/EE.UU.
    [InlineData("fr", "Europe/Paris", "dd/MM/yyyy", "24h")] // Francés/Francia
    [InlineData("de", "Europe/Berlin", "dd.MM.yyyy", "24h")] // Alemán/Alemania
    [InlineData("ja", "Asia/Tokyo", "yyyy/MM/dd", "24h")] // Japonés/Japón
    public void PerfilesDeLocalizacion_DeberianConfigurarseCorrectamente(
        string language, string timeZone, string dateFormat, string timeFormat)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateLocalizationSettings(language, timeZone, dateFormat, timeFormat);

        // Assert
        userSettings.PreferredLanguage.Should().Be(language, $"PreferredLanguage debe ser '{language}'.");
        userSettings.TimeZone.Should().Be(timeZone, $"TimeZone debe ser '{timeZone}'.");
        userSettings.DateFormat.Should().Be(dateFormat, $"DateFormat debe ser '{dateFormat}'.");
        userSettings.TimeFormat.Should().Be(timeFormat, $"TimeFormat debe ser '{timeFormat}'.");
    }

    [Fact]
    public void SecuritySettings_ValoresExtremosDeSessionTimeout_DeberianSerLimitadosCorrectamente()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act & Assert
        userSettings.UpdateSecuritySettings(sessionTimeoutMinutes: 1);
        userSettings.SessionTimeoutMinutes.Should().Be(5, "SessionTimeoutMinutes debe ser clamped a 5 para valores por debajo del mínimo.");

        userSettings.UpdateSecuritySettings(sessionTimeoutMinutes: 600);
        userSettings.SessionTimeoutMinutes.Should().Be(480, "SessionTimeoutMinutes debe ser clamped a 480 para valores por encima del máximo.");

        userSettings.UpdateSecuritySettings(sessionTimeoutMinutes: 0);
        userSettings.SessionTimeoutMinutes.Should().Be(5, "SessionTimeoutMinutes debe ser clamped a 5 para un valor de 0.");
    }

    [Fact]
    public void LocalizationSettings_ConCadenasMuyLargas_DeberiaAlmacenarCorrectamente()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var longString = new string('a', 100); // Excede MaxLength

        // Act - Estos deberían ser manejados por los atributos de validación, pero las pruebas unitarias verifican la lógica de asignación directa
        userSettings.UpdateLocalizationSettings(language: "en-US-very-long-language-code", timeZone: longString, dateFormat: longString, timeFormat: longString);

        // Assert - Esperando valores truncados o manejados por EF Core/Validation si la persistencia ocurre realmente
        userSettings.PreferredLanguage.Should().Be("en-US-ver", "PreferredLanguage debe ser truncado a su longitud máxima.");
        userSettings.TimeZone.Should().Be(new string('a', 50), "TimeZone debe ser truncado a su longitud máxima.");
        userSettings.DateFormat.Should().Be(new string('a', 20), "DateFormat debe ser truncado a su longitud máxima.");
        userSettings.TimeFormat.Should().Be(new string('a', 10), "TimeFormat debe ser truncado a su longitud máxima.");
    }

    [Fact]
    public void NotificationSettings_AlternanciaRapida_DeberiaMantenerEstadoCorrecto()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            userSettings.EmailNotificationsEnabled = !userSettings.EmailNotificationsEnabled;
            userSettings.EmailNotificationsEnabled.Should().Be(i % 2 == 0 ? false : true, "EmailNotificationsEnabled debe alternar correctamente el estado.");
        }
    }

    #endregion
}