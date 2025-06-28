using Accesia.Domain.Entities;
using Accesia.Application.Common.DTOs;

namespace Accesia.Application.Common.Interfaces;

public interface ILogIntegrityService
{
    Task<string> ComputeLogHashAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
    Task<bool> VerifyLogIntegrityAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IntegrityVerificationReport> VerifyLogIntegrityBatchAsync(IEnumerable<SecurityAuditLog> auditLogs, CancellationToken cancellationToken = default);
    Task<bool> AddIntegrityHashAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default);
    Task<List<SecurityAuditLog>> FindCorruptedLogsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<ChainIntegrityReport> VerifyLogChainIntegrityAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
} 