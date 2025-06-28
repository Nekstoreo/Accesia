using System.ComponentModel.DataAnnotations;
using Accesia.Domain.Common;
using Accesia.Domain.Enums;

namespace Accesia.Domain.Entities;

public class UserSettings : AuditableEntity
{
    // Constructor privado para EF Core
    private UserSettings()
    {
    }

    public UserSettings(Guid userId)
    {
        UserId = userId;
    }

    [Required] public Guid UserId { get; set; }

    // Preferencias de notificación
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool InAppNotificationsEnabled { get; set; } = true;

    // Configuraciones específicas por tipo de notificación
    public bool SecurityAlertsEnabled { get; set; } = true;
    public bool LoginActivityNotificationsEnabled { get; set; } = true;
    public bool PasswordChangeNotificationsEnabled { get; set; } = true;
    public bool AccountUpdateNotificationsEnabled { get; set; } = true;
    public bool SystemAnnouncementsEnabled { get; set; } = true;
    public bool DeviceActivityNotificationsEnabled { get; set; } = true;

    // Configuraciones de privacidad
    public PrivacyLevel ProfileVisibility { get; set; } = PrivacyLevel.Private;
    public bool ShowLastLoginTime { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public bool AllowDataCollection { get; set; }
    public bool AllowMarketingEmails { get; set; }

    // Configuraciones regionales y de localización
    [MaxLength(10)] public string PreferredLanguage { get; set; } = "es";

    [MaxLength(50)] public string TimeZone { get; set; } = "America/Bogota";

    [MaxLength(10)] public string DateFormat { get; set; } = "dd/MM/yyyy";

    [MaxLength(10)] public string TimeFormat { get; set; } = "24h";

    // Configuraciones de seguridad
    public bool TwoFactorAuthEnabled { get; set; }
    public bool RequirePasswordChangeOn2FADisable { get; set; } = true;
    public bool LogoutOnPasswordChange { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 60;

    // Navegación
    public User User { get; set; } = null!;

    // Factory method
    public static UserSettings CreateDefault(Guid userId)
    {
        return new UserSettings(userId);
    }

    // Métodos de configuración de notificaciones
    public void EnableNotificationType(NotificationType type, bool enabled = true)
    {
        switch (type)
        {
            case NotificationType.SecurityAlert:
                SecurityAlertsEnabled = enabled;
                break;
            case NotificationType.LoginActivity:
                LoginActivityNotificationsEnabled = enabled;
                break;
            case NotificationType.PasswordChange:
                PasswordChangeNotificationsEnabled = enabled;
                break;
            case NotificationType.AccountUpdate:
                AccountUpdateNotificationsEnabled = enabled;
                break;
            case NotificationType.SystemAnnouncement:
                SystemAnnouncementsEnabled = enabled;
                break;
            case NotificationType.DeviceActivity:
                DeviceActivityNotificationsEnabled = enabled;
                break;
        }
    }

    public void EnableNotificationChannel(NotificationChannel channel, bool enabled = true)
    {
        switch (channel)
        {
            case NotificationChannel.Email:
                EmailNotificationsEnabled = enabled;
                break;
            case NotificationChannel.Sms:
                SmsNotificationsEnabled = enabled;
                break;
            case NotificationChannel.Push:
                PushNotificationsEnabled = enabled;
                break;
            case NotificationChannel.InApp:
                InAppNotificationsEnabled = enabled;
                break;
        }
    }

    public bool IsNotificationEnabled(NotificationType type, NotificationChannel channel)
    {
        var typeEnabled = type switch
        {
            NotificationType.SecurityAlert => SecurityAlertsEnabled,
            NotificationType.LoginActivity => LoginActivityNotificationsEnabled,
            NotificationType.PasswordChange => PasswordChangeNotificationsEnabled,
            NotificationType.AccountUpdate => AccountUpdateNotificationsEnabled,
            NotificationType.SystemAnnouncement => SystemAnnouncementsEnabled,
            NotificationType.DeviceActivity => DeviceActivityNotificationsEnabled,
            _ => false
        };

        var channelEnabled = channel switch
        {
            NotificationChannel.Email => EmailNotificationsEnabled,
            NotificationChannel.Sms => SmsNotificationsEnabled,
            NotificationChannel.Push => PushNotificationsEnabled,
            NotificationChannel.InApp => InAppNotificationsEnabled,
            _ => false
        };

        return typeEnabled && channelEnabled;
    }

    public void UpdatePrivacySettings(PrivacyLevel profileVisibility, bool showLastLoginTime,
        bool showOnlineStatus, bool allowDataCollection,
        bool allowMarketingEmails)
    {
        ProfileVisibility = profileVisibility;
        ShowLastLoginTime = showLastLoginTime;
        ShowOnlineStatus = showOnlineStatus;
        AllowDataCollection = allowDataCollection;
        AllowMarketingEmails = allowMarketingEmails;
    }

    public void UpdateLocalizationSettings(string? preferredLanguage = null, string? timeZone = null,
        string? dateFormat = null, string? timeFormat = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredLanguage))
            PreferredLanguage = preferredLanguage;

        if (!string.IsNullOrWhiteSpace(timeZone))
            TimeZone = timeZone;

        if (!string.IsNullOrWhiteSpace(dateFormat))
            DateFormat = dateFormat;

        if (!string.IsNullOrWhiteSpace(timeFormat))
            TimeFormat = timeFormat;
    }

    public void UpdateSecuritySettings(bool twoFactorAuthEnabled, bool requirePasswordChangeOn2FADisable,
        bool logoutOnPasswordChange, int sessionTimeoutMinutes)
    {
        TwoFactorAuthEnabled = twoFactorAuthEnabled;
        RequirePasswordChangeOn2FADisable = requirePasswordChangeOn2FADisable;
        LogoutOnPasswordChange = logoutOnPasswordChange;
        SessionTimeoutMinutes = Math.Max(5, Math.Min(sessionTimeoutMinutes, 480)); // Entre 5 minutos y 8 horas
    }
}