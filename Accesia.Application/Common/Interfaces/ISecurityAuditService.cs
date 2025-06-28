using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Common.Interfaces;

public interface ISecurityAuditService
{
    Task LogLoginAttemptAsync(Guid? userId, string email, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogPasswordChangeAsync(Guid userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogEmailChangeAsync(Guid userId, string oldEmail, string newEmail, string ipAddress,
        string userAgent, DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogAccountDeletionAsync(Guid userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogRateLimitExceededAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string policyName,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogSuspiciousActivityAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string activityDescription,
        string? failureReason = null, LocationInfo? locationInfo = null,
        Dictionary<string, object>? additionalData = null, CancellationToken cancellationToken = default);

    Task LogUnauthorizedAccessAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string httpMethod, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default);

    Task LogCustomSecurityEventAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);

    Task<IEnumerable<SecurityAuditLog>> GetSecurityEventsAsync(Guid? userId = null,
        string? eventType = null, string? eventCategory = null, DateTime? fromDate = null,
        DateTime? toDate = null, string? severity = null, int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<SecurityAuditLog>> GetCriticalEventsAsync(DateTime? fromDate = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetSecurityEventStatisticsAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    Task AlertCriticalEventAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
}