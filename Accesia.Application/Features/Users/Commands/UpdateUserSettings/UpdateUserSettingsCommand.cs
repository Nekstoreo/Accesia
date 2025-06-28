using MediatR;
using Accesia.Domain.Enums;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.UpdateUserSettings;

public record UpdateUserSettingsCommand(
    Guid UserId,
    NotificationSettings? NotificationSettings = null,
    PrivacySettings? PrivacySettings = null,
    LocalizationSettings? LocalizationSettings = null,
    SecuritySettings? SecuritySettings = null
) : IRequest<UpdateUserSettingsResponse>;

public record NotificationSettings(
    bool? EmailNotificationsEnabled = null,
    bool? SmsNotificationsEnabled = null,
    bool? PushNotificationsEnabled = null,
    bool? InAppNotificationsEnabled = null,
    bool? SecurityAlertsEnabled = null,
    bool? LoginActivityNotificationsEnabled = null,
    bool? PasswordChangeNotificationsEnabled = null,
    bool? AccountUpdateNotificationsEnabled = null,
    bool? SystemAnnouncementsEnabled = null,
    bool? DeviceActivityNotificationsEnabled = null
);

public record PrivacySettings(
    PrivacyLevel? ProfileVisibility = null,
    bool? ShowLastLoginTime = null,
    bool? ShowOnlineStatus = null,
    bool? AllowDataCollection = null,
    bool? AllowMarketingEmails = null
);

public record LocalizationSettings(
    string? PreferredLanguage = null,
    string? TimeZone = null,
    string? DateFormat = null,
    string? TimeFormat = null
);

public record SecuritySettings(
    bool? TwoFactorAuthEnabled = null,
    bool? RequirePasswordChangeOn2FADisable = null,
    bool? LogoutOnPasswordChange = null,
    int? SessionTimeoutMinutes = null
); 