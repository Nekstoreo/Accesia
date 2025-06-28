using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Common.Interfaces;

public interface ISecurityAlertService
{
    Task<bool> ShouldAlertAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
    Task SendAlertAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
    Task<bool> IsUnusualLocationAsync(Guid? userId, LocationInfo? locationInfo, CancellationToken cancellationToken = default);
    Task<SecurityThreatLevel> AnalyzeThreatLevelAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
}

public enum SecurityThreatLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
} 