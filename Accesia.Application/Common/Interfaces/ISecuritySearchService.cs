using Accesia.Domain.Entities;
using Accesia.Application.Common.DTOs;

namespace Accesia.Application.Common.Interfaces;

public interface ISecuritySearchService
{
    Task<SearchResult<SecurityAuditLog>> SearchSecurityEventsAsync(SecuritySearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetSecurityStatisticsAsync(SecuritySearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<List<SecurityTrend>> GetSecurityTrendsAsync(SecuritySearchCriteria criteria, TrendInterval interval, CancellationToken cancellationToken = default);
    Task<List<string>> GetSuspiciousIPsAsync(DateTime fromDate, int minimumFailures = 5, CancellationToken cancellationToken = default);
    Task<List<SecurityAuditLog>> GetRelatedEventsAsync(Guid eventId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
} 