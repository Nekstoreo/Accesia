using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accesia.Infrastructure.Jobs;

/// <summary>
/// Job para rotación automática y limpieza de logs de auditoría de seguridad
/// Maneja el crecimiento del volumen de datos y mantiene la performance del sistema
/// </summary>
public class AuditLogCleanupJob : BackgroundService
{
    private readonly ILogger<AuditLogCleanupJob> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // Ejecutar diariamente
    private readonly IServiceProvider _serviceProvider;

    public AuditLogCleanupJob(IServiceProvider serviceProvider, ILogger<AuditLogCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Esperar un poco antes de la primera ejecución
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza de logs de auditoría");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task DoCleanupAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando limpieza y rotación de logs de auditoría");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var securitySettings = scope.ServiceProvider.GetRequiredService<IOptions<SecuritySettings>>().Value;

        try
        {
            var retentionDate = DateTime.UtcNow.AddDays(-securitySettings.SecurityAudit.LogRetentionDays);
            
            _logger.LogInformation("Eliminando logs de auditoría anteriores a {RetentionDate}", retentionDate);

            // Contar logs antes de la eliminación
            var totalLogsBefore = await context.SecurityAuditLogs.CountAsync(cancellationToken);

            // Archivar logs críticos antes de eliminar (opcional)
            await ArchiveCriticalLogsAsync(context, retentionDate, cancellationToken);

            // Eliminar logs antiguos en lotes para evitar bloqueos largos
            var batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                var logsToDelete = await context.SecurityAuditLogs
                    .Where(sal => sal.OccurredAt < retentionDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!logsToDelete.Any())
                    break;

                context.SecurityAuditLogs.RemoveRange(logsToDelete);
                await context.SaveChangesAsync(cancellationToken);

                totalDeleted += logsToDelete.Count;
                _logger.LogDebug("Eliminado lote de {BatchSize} logs. Total eliminados: {TotalDeleted}", 
                    logsToDelete.Count, totalDeleted);

                // Pequeña pausa para no saturar la base de datos
                await Task.Delay(100, cancellationToken);
            }

            var totalLogsAfter = await context.SecurityAuditLogs.CountAsync(cancellationToken);

            _logger.LogInformation(
                "Limpieza de logs completada. Logs antes: {LogsBefore}, Logs después: {LogsAfter}, Eliminados: {Deleted}", 
                totalLogsBefore, totalLogsAfter, totalDeleted);

            // Optimizar base de datos después de la limpieza
            await OptimizeDatabaseAsync(context, cancellationToken);

            // Generar estadísticas de retención
            await GenerateRetentionStatisticsAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de logs de auditoría");
            throw;
        }
    }

    private async Task ArchiveCriticalLogsAsync(IApplicationDbContext context, DateTime retentionDate, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Contar logs críticos que serán eliminados
            var criticalLogsToArchive = await context.SecurityAuditLogs
                .Where(sal => sal.OccurredAt < retentionDate && 
                             (sal.Severity == "Critical" || sal.Severity == "High"))
                .CountAsync(cancellationToken);

            if (criticalLogsToArchive > 0)
            {
                _logger.LogInformation("Se archivarán {Count} logs críticos antes de eliminar", criticalLogsToArchive);
                
                // TODO: Implementar archivado a sistema externo (Azure Blob, S3, etc.)
                // Por ejemplo: await _archiveService.ArchiveCriticalLogsAsync(logs, cancellationToken);
                
                _logger.LogWarning("Archivado de logs críticos no implementado. {Count} logs críticos se perderán", 
                    criticalLogsToArchive);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el archivado de logs críticos");
        }
    }

    private async Task OptimizeDatabaseAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Optimizando base de datos después de la limpieza");
            
            // Si es SQL Server, ejecutar reorganización de índices
            // Esto es opcional y depende del proveedor de base de datos
            if (context is DbContext dbContext)
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.GetType().Name.Contains("SqlConnection"))
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "ALTER INDEX ALL ON SecurityAuditLogs REORGANIZE", cancellationToken);
                    
                    _logger.LogDebug("Índices reorganizados exitosamente");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron optimizar los índices de la base de datos");
        }
    }

    private async Task GenerateRetentionStatisticsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await context.SecurityAuditLogs
                .GroupBy(sal => sal.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var totalLogs = stats.Sum(s => s.Count);
            var oldestLog = await context.SecurityAuditLogs
                .OrderBy(sal => sal.OccurredAt)
                .Select(sal => sal.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInformation("Estadísticas de retención - Total logs: {TotalLogs}, Log más antiguo: {OldestLog}", 
                totalLogs, oldestLog);

            foreach (var stat in stats)
            {
                _logger.LogDebug("Tipo de evento {EventType}: {Count} logs", stat.EventType, stat.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al generar estadísticas de retención");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit Log Cleanup Job deteniéndose...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Audit Log Cleanup Job detenido");
    }
} 