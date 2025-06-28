using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.DTOs;
using Accesia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Accesia.Infrastructure.Services;

public class LogIntegrityService : ILogIntegrityService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LogIntegrityService> _logger;
    private readonly string _secretKey;

    public LogIntegrityService(IApplicationDbContext context, ILogger<LogIntegrityService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Eliminar valor por defecto - forzar configuración segura
        _secretKey = Environment.GetEnvironmentVariable("LOG_INTEGRITY_SECRET") 
            ?? throw new InvalidOperationException(
                "LOG_INTEGRITY_SECRET environment variable must be configured. " +
                "This is required for log integrity verification and cannot have a default value for security reasons.");
        
        if (_secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "LOG_INTEGRITY_SECRET must be at least 32 characters long for adequate security.");
        }
    }

    public Task<string> ComputeLogHashAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            // Crear representación consistente del log para hash
            var logData = new
            {
                auditLog.Id,
                auditLog.UserId,
                auditLog.EventType,
                auditLog.EventCategory,
                auditLog.Description,
                auditLog.IpAddress,
                auditLog.UserAgent,
                auditLog.Endpoint,
                auditLog.HttpMethod,
                auditLog.IsSuccessful,
                auditLog.Severity,
                OccurredAt = auditLog.OccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                DeviceInfo = new
                {
                    auditLog.DeviceInfo.DeviceType,
                    auditLog.DeviceInfo.OperatingSystem,
                    auditLog.DeviceInfo.Browser,
                    auditLog.DeviceInfo.DeviceFingerprint
                },
                LocationInfo = auditLog.LocationInfo != null ? new
                {
                    auditLog.LocationInfo.Country,
                    auditLog.LocationInfo.City
                } : null,
                AdditionalData = auditLog.AdditionalData.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)
            };

            var jsonString = JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            return Task.FromResult(ComputeHmacSha256(jsonString));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al calcular hash del log {LogId}", auditLog.Id);
            throw;
        }
    }

    public async Task<bool> VerifyLogIntegrityAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            // Si el log no tiene hash guardado, no se puede verificar
            var storedHash = auditLog.AdditionalData.ContainsKey("IntegrityHash") 
                ? auditLog.AdditionalData["IntegrityHash"].ToString()
                : null;

            if (string.IsNullOrEmpty(storedHash))
            {
                _logger.LogWarning("Log {LogId} no tiene hash de integridad almacenado", auditLog.Id);
                return false;
            }

            // Calcular hash actual y comparar
            var computedHash = await ComputeLogHashAsync(auditLog, cancellationToken);
            var isValid = string.Equals(storedHash, computedHash, StringComparison.Ordinal);

            if (!isValid)
            {
                _logger.LogError("Integridad comprometida en log {LogId}. Hash esperado: {Expected}, Hash actual: {Actual}",
                    auditLog.Id, storedHash, computedHash);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar integridad del log {LogId}", auditLog.Id);
            return false;
        }
    }

    public async Task<IntegrityVerificationReport> VerifyLogIntegrityBatchAsync(
        IEnumerable<SecurityAuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        var report = new IntegrityVerificationReport
        {
            StartTime = DateTime.UtcNow,
            TotalLogsChecked = auditLogs.Count()
        };

        try
        {
            _logger.LogInformation("Iniciando verificación de integridad en lote para {Count} logs", report.TotalLogsChecked);

            var verificationTasks = auditLogs.Select(async log =>
            {
                var isValid = await VerifyLogIntegrityAsync(log, cancellationToken);
                return new { Log = log, IsValid = isValid };
            });

            var results = await Task.WhenAll(verificationTasks);

            report.ValidLogs = results.Count(r => r.IsValid);
            report.InvalidLogs = results.Count(r => !r.IsValid);
            report.CorruptedLogIds = results.Where(r => !r.IsValid).Select(r => r.Log.Id).ToList();

            report.EndTime = DateTime.UtcNow;
            report.Duration = report.EndTime - report.StartTime;

            _logger.LogInformation("Verificación completada. Válidos: {Valid}, Inválidos: {Invalid}, Duración: {Duration}ms",
                report.ValidLogs, report.InvalidLogs, report.Duration.TotalMilliseconds);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante verificación de integridad en lote");
            report.EndTime = DateTime.UtcNow;
            report.Duration = report.EndTime - report.StartTime;
            report.HasErrors = true;
            return report;
        }
    }

    public async Task<bool> AddIntegrityHashAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            var hash = await ComputeLogHashAsync(auditLog, cancellationToken);
            auditLog.AddAdditionalData("IntegrityHash", hash);
            auditLog.AddAdditionalData("IntegrityTimestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar hash de integridad al log {LogId}", auditLog.Id);
            return false;
        }
    }

    public async Task<List<SecurityAuditLog>> FindCorruptedLogsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logsToCheck = await _context.SecurityAuditLogs
                .Where(sal => sal.OccurredAt >= fromDate && sal.OccurredAt <= toDate)
                .ToListAsync(cancellationToken);

            var corruptedLogs = new List<SecurityAuditLog>();

            foreach (var log in logsToCheck)
            {
                var isValid = await VerifyLogIntegrityAsync(log, cancellationToken);
                if (!isValid)
                {
                    corruptedLogs.Add(log);
                }
            }

            _logger.LogInformation("Verificación de integridad completada. {CorruptedCount} logs corruptos encontrados de {TotalCount} verificados",
                corruptedLogs.Count, logsToCheck.Count);

            return corruptedLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar logs corruptos entre {FromDate} y {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<ChainIntegrityReport> VerifyLogChainIntegrityAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var report = new ChainIntegrityReport
        {
            StartTime = startTime,
            FromDate = fromDate,
            ToDate = toDate,
            IsValid = true,
            BrokenLinks = new List<ChainBreak>()
        };

        try
        {
            _logger.LogInformation("Iniciando verificación de integridad de cadena desde {FromDate} hasta {ToDate}", fromDate, toDate);

            // Contar total de logs para el reporte
            var totalCount = await _context.SecurityAuditLogs
                .Where(sal => sal.OccurredAt >= fromDate && sal.OccurredAt <= toDate)
                .CountAsync(cancellationToken);

            report.TotalLogsInChain = totalCount;

            if (totalCount == 0)
            {
                _logger.LogInformation("No hay logs en el rango especificado");
                report.EndTime = DateTime.UtcNow;
                report.Duration = report.EndTime - report.StartTime;
                return report;
            }

            // Procesar en lotes para evitar consumo excesivo de memoria
            const int batchSize = 1000;
            var processedCount = 0;
            SecurityAuditLog? previousLog = null;

            // Obtener el primer log para inicializar la cadena
            var firstLog = await _context.SecurityAuditLogs
                .Where(sal => sal.OccurredAt >= fromDate && sal.OccurredAt <= toDate)
                .OrderBy(sal => sal.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstLog != null)
            {
                previousLog = firstLog;
                processedCount = 1;
            }

            // Procesar el resto en lotes
            while (processedCount < totalCount)
            {
                var batch = await _context.SecurityAuditLogs
                    .Where(sal => sal.OccurredAt >= fromDate && sal.OccurredAt <= toDate)
                    .OrderBy(sal => sal.OccurredAt)
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any())
                    break;

                foreach (var currentLog in batch)
                {
                    if (previousLog != null)
                    {
                        // Verificar integridad del log actual
                        var isCurrentValid = await VerifyLogIntegrityAsync(currentLog, cancellationToken);
                        if (!isCurrentValid)
                        {
                            report.BrokenLinks.Add(new ChainBreak
                            {
                                LogId = currentLog.Id,
                                OccurredAt = currentLog.OccurredAt,
                                BreakType = ChainBreakType.CorruptedLog,
                                Description = "Log integrity hash verification failed"
                            });
                            report.IsValid = false;
                        }

                        // Verificar continuidad temporal (detectar gaps sospechosos)
                        var timeDifference = currentLog.OccurredAt - previousLog.OccurredAt;
                        if (timeDifference > TimeSpan.FromHours(24) && 
                            previousLog.OccurredAt.Date != currentLog.OccurredAt.Date)
                        {
                            // Gap de más de 24 horas podría indicar logs faltantes
                            report.BrokenLinks.Add(new ChainBreak
                            {
                                LogId = currentLog.Id,
                                OccurredAt = currentLog.OccurredAt,
                                BreakType = ChainBreakType.SuspiciousGap,
                                Description = $"Suspicious time gap of {timeDifference.TotalHours:F1} hours between logs"
                            });
                        }

                        // Verificar orden cronológico
                        if (currentLog.OccurredAt < previousLog.OccurredAt)
                        {
                            report.BrokenLinks.Add(new ChainBreak
                            {
                                LogId = currentLog.Id,
                                OccurredAt = currentLog.OccurredAt,
                                BreakType = ChainBreakType.TimestampAnomaly,
                                Description = "Log timestamp is earlier than previous log"
                            });
                            report.IsValid = false;
                        }
                    }

                    previousLog = currentLog;
                }

                processedCount += batch.Count;
                
                // Log de progreso para lotes grandes
                if (totalCount > 5000)
                {
                    _logger.LogDebug("Verificación de integridad: {ProcessedCount}/{TotalCount} logs procesados", 
                        processedCount, totalCount);
                }

                // Pequeña pausa para no saturar la base de datos
                if (batch.Count == batchSize)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }

            report.EndTime = DateTime.UtcNow;
            report.Duration = report.EndTime - report.StartTime;

            if (report.BrokenLinks.Any())
            {
                report.HasErrors = true;
                _logger.LogWarning("Verificación de cadena completada con {ErrorCount} errores encontrados", 
                    report.BrokenLinks.Count);
            }
            else
            {
                _logger.LogInformation("Verificación de cadena completada exitosamente. {TotalCount} logs verificados en {Duration}", 
                    totalCount, report.Duration);
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la verificación de integridad de cadena");
            report.HasErrors = true;
            report.EndTime = DateTime.UtcNow;
            report.Duration = report.EndTime - report.StartTime;
            throw;
        }
    }

    private string ComputeHmacSha256(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }
}

 