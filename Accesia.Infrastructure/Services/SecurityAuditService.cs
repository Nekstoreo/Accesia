using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Services;

public class SecurityAuditService : ISecurityAuditService
{
    private static readonly HashSet<string> CriticalSeverities = new() { "Critical", "High" };
    private static readonly HashSet<string> CriticalAuditEvents = new()
    {
        "password_changed",
        "admin_action_performed",
        "privilege_escalation",
        "account_deletion_confirmed",
        "security_settings_changed"
    };
    private readonly ISecurityAlertService _alertService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogIntegrityService _integrityService;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(
        ApplicationDbContext context,
        ILogger<SecurityAuditService> logger,
        IEmailService emailService,
        ISecurityAlertService alertService,
        ILogIntegrityService integrityService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _alertService = alertService;
        _integrityService = integrityService;
    }

    public async Task LogLoginAttemptAsync(Guid? userId, string email, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateLoginAttempt(
            userId, email, ipAddress, userAgent, deviceInfo, isSuccessful, failureReason, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        // Log estructurado para Serilog
        if (isSuccessful)
            _logger.LogInformation("Login exitoso - Usuario: {Email}, IP: {IpAddress}, Device: {DeviceType}",
                email, ipAddress, deviceInfo.DeviceType);
        else
            _logger.LogWarning("Login fallido - Usuario: {Email}, IP: {IpAddress}, Razón: {FailureReason}",
                email, ipAddress, failureReason);
    }

    public async Task LogPasswordChangeAsync(Guid userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreatePasswordChange(
            userId, ipAddress, userAgent, deviceInfo, isSuccessful, failureReason, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogInformation("Cambio de contraseña - Usuario: {UserId}, IP: {IpAddress}, Exitoso: {IsSuccessful}",
            userId, ipAddress, isSuccessful);
    }

    public async Task LogEmailChangeAsync(Guid userId, string oldEmail, string newEmail, string ipAddress,
        string userAgent, DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateEmailChange(
            userId, oldEmail, newEmail, ipAddress, userAgent, deviceInfo, isSuccessful, failureReason, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogInformation("Cambio de email - Usuario: {UserId}, De: {OldEmail}, A: {NewEmail}, IP: {IpAddress}",
            userId, oldEmail, newEmail, ipAddress);
    }

    public async Task LogAccountDeletionAsync(Guid userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, bool isSuccessful, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateAccountDeletion(
            userId, ipAddress, userAgent, deviceInfo, isSuccessful, failureReason, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogWarning(
            "Solicitud de eliminación de cuenta - Usuario: {UserId}, IP: {IpAddress}, Exitoso: {IsSuccessful}",
            userId, ipAddress, isSuccessful);
    }

    public async Task LogRateLimitExceededAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string policyName,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateRateLimitExceeded(
            userId, ipAddress, userAgent, deviceInfo, endpoint, policyName, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogWarning("Rate limit excedido - IP: {IpAddress}, Endpoint: {Endpoint}, Política: {PolicyName}",
            ipAddress, endpoint, policyName);
    }

    public async Task LogSuspiciousActivityAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string activityDescription, string? failureReason = null,
        LocationInfo? locationInfo = null, Dictionary<string, object>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateSuspiciousActivity(
            userId, ipAddress, userAgent, deviceInfo, endpoint, activityDescription, failureReason, locationInfo,
            additionalData);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogError(
            "Actividad sospechosa detectada - IP: {IpAddress}, Endpoint: {Endpoint}, Descripción: {ActivityDescription}",
            ipAddress, endpoint, activityDescription);
    }

    public async Task LogUnauthorizedAccessAsync(Guid? userId, string ipAddress, string userAgent,
        DeviceInfo deviceInfo, string endpoint, string httpMethod, string? failureReason = null,
        LocationInfo? locationInfo = null, CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateUnauthorizedAccess(
            userId, ipAddress, userAgent, deviceInfo, endpoint, httpMethod, failureReason, locationInfo);

        await LogSecurityEventInternalAsync(auditLog, cancellationToken);

        _logger.LogWarning("Acceso no autorizado - IP: {IpAddress}, Endpoint: {Endpoint}, Método: {HttpMethod}",
            ipAddress, endpoint, httpMethod);
    }

    public async Task LogCustomSecurityEventAsync(SecurityAuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        await LogSecurityEventInternalAsync(auditLog, cancellationToken);
    }

    public async Task<IEnumerable<SecurityAuditLog>> GetSecurityEventsAsync(Guid? userId = null,
        string? eventType = null, string? eventCategory = null, DateTime? fromDate = null,
        DateTime? toDate = null, string? severity = null, int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Include(sal => sal.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(sal => sal.UserId == userId.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(sal => sal.EventType == eventType);

        if (!string.IsNullOrEmpty(eventCategory))
            query = query.Where(sal => sal.EventCategory == eventCategory);

        if (fromDate.HasValue)
            query = query.Where(sal => sal.OccurredAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(sal => sal.OccurredAt <= toDate.Value);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(sal => sal.Severity == severity);

        return await query
            .OrderByDescending(sal => sal.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityAuditLog>> GetCriticalEventsAsync(DateTime? fromDate = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Include(sal => sal.User)
            .Where(sal => CriticalSeverities.Contains(sal.Severity));

        if (fromDate.HasValue)
            query = query.Where(sal => sal.OccurredAt >= fromDate.Value);

        return await query
            .OrderByDescending(sal => sal.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetSecurityEventStatisticsAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.SecurityAuditLogs
            .Where(sal => sal.OccurredAt >= fromDate && sal.OccurredAt <= toDate)
            .GroupBy(sal => sal.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return events.ToDictionary(e => e.EventType, e => e.Count);
    }

    public async Task AlertCriticalEventAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            // Usar el nuevo servicio de alertas inteligente
            var shouldAlert = await _alertService.ShouldAlertAsync(auditLog, cancellationToken);
            if (shouldAlert) await _alertService.SendAlertAsync(auditLog, cancellationToken);

            // Log crítico tradicional
            _logger.LogCritical("ALERTA CRÍTICA DE SEGURIDAD: {EventType} - {Description} - IP: {IpAddress}",
                auditLog.EventType, auditLog.Description, auditLog.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta crítica de seguridad");
        }
    }

    private async Task LogSecurityEventInternalAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        try
        {
            // Agregar hash de integridad al log
            await _integrityService.AddIntegrityHashAsync(auditLog, cancellationToken);

            _context.SecurityAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            // Procesar alertas si es necesario
            if (CriticalSeverities.Contains(auditLog.Severity))
                _ = Task.Run(async () => await AlertCriticalEventAsync(auditLog, cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al guardar log de auditoría para evento {EventType}", auditLog.EventType);
            
            // Para eventos críticos, propagar la excepción para detener la operación principal
            if (CriticalAuditEvents.Contains(auditLog.EventType))
            {
                _logger.LogCritical(
                    "FALLO CRÍTICO: No se pudo auditar evento crítico {EventType}. La operación debe ser revertida por seguridad.",
                    auditLog.EventType);
                
                throw new InvalidOperationException(
                    $"Critical audit failure for event {auditLog.EventType}. Operation cannot proceed without proper audit trail.",
                    ex);
            }
            
            // Para eventos no críticos, intentar logging alternativo pero no detener la operación
            await TryAlternativeLoggingAsync(auditLog, ex, cancellationToken);
        }
    }

    private async Task TryAlternativeLoggingAsync(SecurityAuditLog auditLog, Exception originalException, CancellationToken cancellationToken)
    {
        try
        {
            // Log estructurado como respaldo cuando falla la base de datos
            _logger.LogError(originalException,
                "AUDIT_BACKUP: {EventType} | User: {UserId} | IP: {IpAddress} | Time: {OccurredAt} | Success: {IsSuccessful} | Description: {Description}",
                auditLog.EventType, auditLog.UserId, auditLog.IpAddress, auditLog.OccurredAt, auditLog.IsSuccessful, auditLog.Description);

            // TODO: Implementar logging a sistema externo (Azure Monitor, Splunk, etc.)
            // await _externalLoggingService.LogSecurityEventAsync(auditLog, cancellationToken);
        }
        catch (Exception backupEx)
        {
            _logger.LogCritical(backupEx, "FALLO TOTAL DE AUDITORÍA: No se pudo registrar evento {EventType} ni en base de datos ni en sistema de respaldo", auditLog.EventType);
        }
    }
}