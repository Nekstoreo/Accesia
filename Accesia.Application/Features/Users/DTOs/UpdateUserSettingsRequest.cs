using System.ComponentModel.DataAnnotations;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.DTOs;

public class UpdateUserSettingsRequest
{
    [Required]
    public Guid UserId { get; set; }

    public NotificationSettingsDto? NotificationSettings { get; set; }
    public PrivacySettingsDto? PrivacySettings { get; set; }
    public LocalizationSettingsDto? LocalizationSettings { get; set; }
    public SecuritySettingsDto? SecuritySettings { get; set; }
}

public class NotificationSettingsDto
{
    public bool? EmailNotificationsEnabled { get; set; }
    public bool? SmsNotificationsEnabled { get; set; }
    public bool? PushNotificationsEnabled { get; set; }
    public bool? InAppNotificationsEnabled { get; set; }
    public bool? SecurityAlertsEnabled { get; set; }
    public bool? LoginActivityNotificationsEnabled { get; set; }
    public bool? PasswordChangeNotificationsEnabled { get; set; }
    public bool? AccountUpdateNotificationsEnabled { get; set; }
    public bool? SystemAnnouncementsEnabled { get; set; }
    public bool? DeviceActivityNotificationsEnabled { get; set; }
}

public class PrivacySettingsDto
{
    [EnumDataType(typeof(PrivacyLevel))]
    public PrivacyLevel? ProfileVisibility { get; set; }
    public bool? ShowLastLoginTime { get; set; }
    public bool? ShowOnlineStatus { get; set; }
    public bool? AllowDataCollection { get; set; }
    public bool? AllowMarketingEmails { get; set; }
}

public class LocalizationSettingsDto
{
    [MaxLength(10)]
    public string? PreferredLanguage { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; }

    [MaxLength(10)]
    public string? DateFormat { get; set; }

    [MaxLength(10)]
    public string? TimeFormat { get; set; }
}

public class SecuritySettingsDto
{
    public bool? TwoFactorAuthEnabled { get; set; }
    public bool? RequirePasswordChangeOn2FADisable { get; set; }
    public bool? LogoutOnPasswordChange { get; set; }

    [Range(5, 480)]
    public int? SessionTimeoutMinutes { get; set; }
} 