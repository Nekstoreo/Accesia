using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accesia.Application.Features.Users.Queries.GetUserSettings;

public class GetUserSettingsHandler : IRequestHandler<GetUserSettingsQuery, GetUserSettingsResponse>
{
    private readonly IApplicationDbContext _context;

    public GetUserSettingsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetUserSettingsResponse> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        // Asegurar que existan las configuraciones
        user.EnsureSettingsExist();
        var settings = user.GetSettings();

        return new GetUserSettingsResponse
        {
            UserId = user.Id,
            NotificationSettings = new NotificationSettingsResponse
            {
                EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
                SmsNotificationsEnabled = settings.SmsNotificationsEnabled,
                PushNotificationsEnabled = settings.PushNotificationsEnabled,
                InAppNotificationsEnabled = settings.InAppNotificationsEnabled,
                SecurityAlertsEnabled = settings.SecurityAlertsEnabled,
                LoginActivityNotificationsEnabled = settings.LoginActivityNotificationsEnabled,
                PasswordChangeNotificationsEnabled = settings.PasswordChangeNotificationsEnabled,
                AccountUpdateNotificationsEnabled = settings.AccountUpdateNotificationsEnabled,
                SystemAnnouncementsEnabled = settings.SystemAnnouncementsEnabled,
                DeviceActivityNotificationsEnabled = settings.DeviceActivityNotificationsEnabled
            },
            PrivacySettings = new PrivacySettingsResponse
            {
                ProfileVisibility = settings.ProfileVisibility,
                ShowLastLoginTime = settings.ShowLastLoginTime,
                ShowOnlineStatus = settings.ShowOnlineStatus,
                AllowDataCollection = settings.AllowDataCollection,
                AllowMarketingEmails = settings.AllowMarketingEmails
            },
            LocalizationSettings = new LocalizationSettingsResponse
            {
                PreferredLanguage = settings.PreferredLanguage,
                TimeZone = settings.TimeZone,
                DateFormat = settings.DateFormat,
                TimeFormat = settings.TimeFormat
            },
            SecuritySettings = new SecuritySettingsResponse
            {
                TwoFactorAuthEnabled = settings.TwoFactorAuthEnabled,
                RequirePasswordChangeOn2FADisable = settings.RequirePasswordChangeOn2FADisable,
                LogoutOnPasswordChange = settings.LogoutOnPasswordChange,
                SessionTimeoutMinutes = settings.SessionTimeoutMinutes
            }
        };
    }
}