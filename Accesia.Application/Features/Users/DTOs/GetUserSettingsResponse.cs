using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.DTOs;

public class GetUserSettingsResponse
{
    public Guid UserId { get; set; }
    public NotificationSettingsResponse NotificationSettings { get; set; } = new();
    public PrivacySettingsResponse PrivacySettings { get; set; } = new();
    public LocalizationSettingsResponse LocalizationSettings { get; set; } = new();
    public SecuritySettingsResponse SecuritySettings { get; set; } = new();
}

public class NotificationSettingsResponse
{
    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
    public bool SecurityAlertsEnabled { get; set; }
    public bool LoginActivityNotificationsEnabled { get; set; }
    public bool PasswordChangeNotificationsEnabled { get; set; }
    public bool AccountUpdateNotificationsEnabled { get; set; }
    public bool SystemAnnouncementsEnabled { get; set; }
    public bool DeviceActivityNotificationsEnabled { get; set; }
}

public class PrivacySettingsResponse
{
    public PrivacyLevel ProfileVisibility { get; set; }
    public bool ShowLastLoginTime { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public bool AllowDataCollection { get; set; }
    public bool AllowMarketingEmails { get; set; }
}

public class LocalizationSettingsResponse
{
    public string PreferredLanguage { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string DateFormat { get; set; } = string.Empty;
    public string TimeFormat { get; set; } = string.Empty;
}

public class SecuritySettingsResponse
{
    public bool TwoFactorAuthEnabled { get; set; }
    public bool RequirePasswordChangeOn2FADisable { get; set; }
    public bool LogoutOnPasswordChange { get; set; }
    public int SessionTimeoutMinutes { get; set; }
}