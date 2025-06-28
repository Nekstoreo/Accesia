using MediatR;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.Entities;
using Accesia.Application.Features.Users.DTOs;
using InvalidTimeZoneException = System.InvalidTimeZoneException;

namespace Accesia.Application.Features.Users.Commands.UpdateUserSettings;

public class UpdateUserSettingsHandler : IRequestHandler<UpdateUserSettingsCommand, UpdateUserSettingsResponse>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserSettingsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateUserSettingsResponse> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        // Asegurar que existan las configuraciones
        user.EnsureSettingsExist();
        var settings = user.GetSettings();

        var updatedSections = new List<string>();

        // Actualizar configuraciones de notificación
        if (request.NotificationSettings != null)
        {
            UpdateNotificationSettings(settings, request.NotificationSettings);
            updatedSections.Add("Notificaciones");
        }

        // Actualizar configuraciones de privacidad
        if (request.PrivacySettings != null)
        {
            UpdatePrivacySettings(settings, request.PrivacySettings);
            updatedSections.Add("Privacidad");
        }

        // Actualizar configuraciones de localización
        if (request.LocalizationSettings != null)
        {
            UpdateLocalizationSettings(settings, request.LocalizationSettings);
            updatedSections.Add("Localización");
        }

        // Actualizar configuraciones de seguridad
        if (request.SecuritySettings != null)
        {
            UpdateSecuritySettings(settings, request.SecuritySettings);
            updatedSections.Add("Seguridad");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateUserSettingsResponse
        {
            UserId = user.Id,
            UpdatedSections = updatedSections,
            Message = $"Configuraciones actualizadas: {string.Join(", ", updatedSections)}",
            Timestamp = DateTime.UtcNow
        };
    }

    private static void UpdateNotificationSettings(UserSettings settings, NotificationSettings notificationSettings)
    {
        if (notificationSettings.EmailNotificationsEnabled.HasValue)
            settings.EmailNotificationsEnabled = notificationSettings.EmailNotificationsEnabled.Value;

        if (notificationSettings.SmsNotificationsEnabled.HasValue)
            settings.SmsNotificationsEnabled = notificationSettings.SmsNotificationsEnabled.Value;

        if (notificationSettings.PushNotificationsEnabled.HasValue)
            settings.PushNotificationsEnabled = notificationSettings.PushNotificationsEnabled.Value;

        if (notificationSettings.InAppNotificationsEnabled.HasValue)
            settings.InAppNotificationsEnabled = notificationSettings.InAppNotificationsEnabled.Value;

        if (notificationSettings.SecurityAlertsEnabled.HasValue)
            settings.SecurityAlertsEnabled = notificationSettings.SecurityAlertsEnabled.Value;

        if (notificationSettings.LoginActivityNotificationsEnabled.HasValue)
            settings.LoginActivityNotificationsEnabled = notificationSettings.LoginActivityNotificationsEnabled.Value;

        if (notificationSettings.PasswordChangeNotificationsEnabled.HasValue)
            settings.PasswordChangeNotificationsEnabled = notificationSettings.PasswordChangeNotificationsEnabled.Value;

        if (notificationSettings.AccountUpdateNotificationsEnabled.HasValue)
            settings.AccountUpdateNotificationsEnabled = notificationSettings.AccountUpdateNotificationsEnabled.Value;

        if (notificationSettings.SystemAnnouncementsEnabled.HasValue)
            settings.SystemAnnouncementsEnabled = notificationSettings.SystemAnnouncementsEnabled.Value;

        if (notificationSettings.DeviceActivityNotificationsEnabled.HasValue)
            settings.DeviceActivityNotificationsEnabled = notificationSettings.DeviceActivityNotificationsEnabled.Value;
    }

    private static void UpdatePrivacySettings(UserSettings settings, PrivacySettings privacySettings)
    {
        settings.UpdatePrivacySettings(
            privacySettings.ProfileVisibility ?? settings.ProfileVisibility,
            privacySettings.ShowLastLoginTime ?? settings.ShowLastLoginTime,
            privacySettings.ShowOnlineStatus ?? settings.ShowOnlineStatus,
            privacySettings.AllowDataCollection ?? settings.AllowDataCollection,
            privacySettings.AllowMarketingEmails ?? settings.AllowMarketingEmails
        );
    }

    private static void UpdateLocalizationSettings(UserSettings settings, LocalizationSettings localizationSettings)
    {
        // Validar zona horaria si se proporciona
        if (!string.IsNullOrEmpty(localizationSettings.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(localizationSettings.TimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new InvalidTimeZoneException(settings.UserId.ToString(), new Exception(localizationSettings.TimeZone));
            }
        }

        // Validar código de idioma si se proporciona
        if (!string.IsNullOrEmpty(localizationSettings.PreferredLanguage))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(localizationSettings.PreferredLanguage, @"^[a-z]{2}(-[A-Z]{2})?$"))
            {
                throw new InvalidLanguageCodeException(settings.UserId.ToString(), localizationSettings.PreferredLanguage);
            }
        }

        // Validar formato de fecha si se proporciona
        if (!string.IsNullOrEmpty(localizationSettings.DateFormat))
        {
            var validFormats = new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (!validFormats.Contains(localizationSettings.DateFormat))
            {
                throw new InvalidDateFormatException(settings.UserId.ToString(), localizationSettings.DateFormat);
            }
        }

        // Validar formato de tiempo si se proporciona
        if (!string.IsNullOrEmpty(localizationSettings.TimeFormat))
        {
            if (localizationSettings.TimeFormat is not ("12h" or "24h"))
            {
                throw new InvalidTimeFormatException(settings.UserId.ToString(), localizationSettings.TimeFormat);
            }
        }

        settings.UpdateLocalizationSettings(
            localizationSettings.PreferredLanguage,
            localizationSettings.TimeZone,
            localizationSettings.DateFormat,
            localizationSettings.TimeFormat
        );
    }

    private static void UpdateSecuritySettings(UserSettings settings, SecuritySettings securitySettings)
    {
        // Validar timeout de sesión si se proporciona
        if (securitySettings.SessionTimeoutMinutes.HasValue)
        {
            var timeout = securitySettings.SessionTimeoutMinutes.Value;
            if (timeout < 5 || timeout > 480)
            {
                throw new InvalidSessionTimeoutException(settings.UserId.ToString(), timeout);
            }
        }

        // Validar configuraciones conflictivas
        var twoFactorEnabled = securitySettings.TwoFactorAuthEnabled ?? settings.TwoFactorAuthEnabled;
        var requirePasswordChange = securitySettings.RequirePasswordChangeOn2FADisable ?? settings.RequirePasswordChangeOn2FADisable;

        if (!twoFactorEnabled && requirePasswordChange)
        {
            throw new ConflictingSecuritySettingsException(
                settings.UserId.ToString(), 
                "No se puede requerir cambio de contraseña al deshabilitar 2FA si 2FA no está habilitado");
        }

        settings.UpdateSecuritySettings(
            twoFactorEnabled,
            requirePasswordChange,
            securitySettings.LogoutOnPasswordChange ?? settings.LogoutOnPasswordChange,
            securitySettings.SessionTimeoutMinutes ?? settings.SessionTimeoutMinutes
        );
    }
} 