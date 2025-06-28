using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.DTOs;
using Accesia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Accesia.Infrastructure.Services;

public class SecuritySearchService : ISecuritySearchService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SecuritySearchService> _logger;

    public SecuritySearchService(IApplicationDbContext context, ILogger<SecuritySearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchResult<SecurityAuditLog>> SearchSecurityEventsAsync(SecuritySearchCriteria criteria, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando búsqueda de eventos de seguridad con criterios: {Criteria}", 
                System.Text.Json.JsonSerializer.Serialize(criteria));

            var query = BuildSearchQuery(criteria);
            
            // Obtener el total de resultados
            var totalCount = await query.CountAsync(cancellationToken);

            // Aplicar paginación y obtener resultados
            var events = await query
                .OrderByDescending(sal => sal.OccurredAt)
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .Include(sal => sal.User)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Búsqueda completada. {TotalCount} eventos encontrados, mostrando {PageSize} en página {PageNumber}", 
                totalCount, events.Count, criteria.PageNumber);

            return new SearchResult<SecurityAuditLog>
            {
                Items = events,
                TotalCount = totalCount,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la búsqueda de eventos de seguridad");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetSecurityStatisticsAsync(SecuritySearchCriteria criteria, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildSearchQuery(criteria);

            var statistics = new Dictionary<string, object>();

            // Estadísticas por tipo de evento
            var eventTypeStats = await query
                .GroupBy(sal => sal.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            statistics["EventTypes"] = eventTypeStats;

            // Estadísticas por severidad
            var severityStats = await query
                .GroupBy(sal => sal.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            statistics["Severities"] = severityStats;

            // Estadísticas por éxito/fallo
            var successStats = await query
                .GroupBy(sal => sal.IsSuccessful)
                .Select(g => new { IsSuccessful = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            statistics["SuccessRates"] = successStats;

            // Top IPs más activas
            var topIPs = await query
                .GroupBy(sal => sal.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            statistics["TopIPs"] = topIPs;

            // Actividad por hora del día
            var hourlyActivity = await query
                .GroupBy(sal => sal.OccurredAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToListAsync(cancellationToken);

            statistics["HourlyActivity"] = hourlyActivity;

            // Países más frecuentes (si hay información de ubicación)
            var countryStats = await query
                .Where(sal => sal.LocationInfo != null)
                .GroupBy(sal => sal.LocationInfo!.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            statistics["TopCountries"] = countryStats;

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar estadísticas de seguridad");
            throw;
        }
    }

    public async Task<List<SecurityTrend>> GetSecurityTrendsAsync(SecuritySearchCriteria criteria, 
        TrendInterval interval, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildSearchQuery(criteria);

            Expression<Func<SecurityAuditLog, object>> groupSelector = interval switch
            {
                TrendInterval.Hourly => sal => new { sal.OccurredAt.Year, sal.OccurredAt.Month, sal.OccurredAt.Day, sal.OccurredAt.Hour },
                TrendInterval.Daily => sal => new { sal.OccurredAt.Year, sal.OccurredAt.Month, sal.OccurredAt.Day },
                TrendInterval.Weekly => sal => new { Year = sal.OccurredAt.Year, Week = sal.OccurredAt.DayOfYear / 7 },
                TrendInterval.Monthly => sal => new { sal.OccurredAt.Year, sal.OccurredAt.Month },
                _ => sal => new { sal.OccurredAt.Year, sal.OccurredAt.Month, sal.OccurredAt.Day }
            };

            var trends = await query
                .GroupBy(groupSelector)
                .Select(g => new SecurityTrend
                {
                    Period = g.Key.ToString()!,
                    TotalEvents = g.Count(),
                    SuccessfulEvents = g.Count(sal => sal.IsSuccessful),
                    FailedEvents = g.Count(sal => !sal.IsSuccessful),
                    CriticalEvents = g.Count(sal => sal.Severity == "Critical"),
                    HighSeverityEvents = g.Count(sal => sal.Severity == "High")
                })
                .OrderBy(t => t.Period)
                .ToListAsync(cancellationToken);

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar tendencias de seguridad");
            throw;
        }
    }

    public async Task<List<string>> GetSuspiciousIPsAsync(DateTime fromDate, int minimumFailures = 5, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var suspiciousIPs = await _context.SecurityAuditLogs
                .Where(sal => sal.OccurredAt >= fromDate && !sal.IsSuccessful)
                .GroupBy(sal => sal.IpAddress)
                .Where(g => g.Count() >= minimumFailures)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Encontradas {Count} IPs sospechosas con al menos {MinFailures} fallos desde {FromDate}", 
                suspiciousIPs.Count, minimumFailures, fromDate);

            return suspiciousIPs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener IPs sospechosas");
            throw;
        }
    }

    public async Task<List<SecurityAuditLog>> GetRelatedEventsAsync(Guid eventId, TimeSpan timeWindow, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseEvent = await _context.SecurityAuditLogs
                .FirstOrDefaultAsync(sal => sal.Id == eventId, cancellationToken);

            if (baseEvent == null)
                return new List<SecurityAuditLog>();

            var windowStart = baseEvent.OccurredAt.Subtract(timeWindow);
            var windowEnd = baseEvent.OccurredAt.Add(timeWindow);

            var relatedEvents = await _context.SecurityAuditLogs
                .Where(sal => sal.Id != eventId &&
                             sal.OccurredAt >= windowStart &&
                             sal.OccurredAt <= windowEnd &&
                             (sal.IpAddress == baseEvent.IpAddress || 
                              sal.UserId == baseEvent.UserId))
                .OrderBy(sal => sal.OccurredAt)
                .Include(sal => sal.User)
                .ToListAsync(cancellationToken);

            return relatedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener eventos relacionados para evento {EventId}", eventId);
            throw;
        }
    }

    private IQueryable<SecurityAuditLog> BuildSearchQuery(SecuritySearchCriteria criteria)
    {
        var query = _context.SecurityAuditLogs.AsQueryable();

        // Filtros básicos
        if (criteria.UserId.HasValue)
            query = query.Where(sal => sal.UserId == criteria.UserId.Value);

        if (!string.IsNullOrEmpty(criteria.EventType))
            query = query.Where(sal => sal.EventType == criteria.EventType);

        if (!string.IsNullOrEmpty(criteria.EventCategory))
            query = query.Where(sal => sal.EventCategory == criteria.EventCategory);

        if (!string.IsNullOrEmpty(criteria.Severity))
            query = query.Where(sal => sal.Severity == criteria.Severity);

        if (!string.IsNullOrEmpty(criteria.IpAddress))
            query = query.Where(sal => sal.IpAddress == criteria.IpAddress);

        if (criteria.IsSuccessful.HasValue)
            query = query.Where(sal => sal.IsSuccessful == criteria.IsSuccessful.Value);

        // Filtros de fecha
        if (criteria.FromDate.HasValue)
            query = query.Where(sal => sal.OccurredAt >= criteria.FromDate.Value);

        if (criteria.ToDate.HasValue)
            query = query.Where(sal => sal.OccurredAt <= criteria.ToDate.Value);

        // Búsqueda de texto libre
        if (!string.IsNullOrEmpty(criteria.SearchText))
        {
            var searchText = criteria.SearchText.ToLower();
            query = query.Where(sal => 
                sal.Description.ToLower().Contains(searchText) ||
                sal.EventType.ToLower().Contains(searchText) ||
                sal.EventCategory.ToLower().Contains(searchText) ||
                (sal.FailureReason != null && sal.FailureReason.ToLower().Contains(searchText)));
        }

        // Filtros de ubicación
        if (!string.IsNullOrEmpty(criteria.Country))
            query = query.Where(sal => sal.LocationInfo != null && sal.LocationInfo.Country == criteria.Country);

        if (!string.IsNullOrEmpty(criteria.City))
            query = query.Where(sal => sal.LocationInfo != null && sal.LocationInfo.City == criteria.City);

        // Filtros de dispositivo
        if (!string.IsNullOrEmpty(criteria.DeviceType))
            query = query.Where(sal => sal.DeviceInfo.DeviceType.ToString() == criteria.DeviceType);

        if (!string.IsNullOrEmpty(criteria.OperatingSystem))
            query = query.Where(sal => sal.DeviceInfo.OperatingSystem.Contains(criteria.OperatingSystem));

        return query;
    }
}

 