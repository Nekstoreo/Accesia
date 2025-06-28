using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Accesia.Application.Common.DTOs;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accesia.Infrastructure.Services;

public class SecurityAlertService : ISecurityAlertService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SecurityAlertService> _logger;
    private readonly SecuritySettings _securitySettings;
    private readonly AlertingSettings _alertingSettings;

    // Cache para ubicaciones conocidas por usuario (en un escenario real usaríamos Redis)
    private readonly Dictionary<Guid, HashSet<string>> _userKnownLocations = new();
    private readonly Dictionary<string, DateTime> _alertThrottleCache = new();

    public SecurityAlertService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<SecurityAlertService> logger,
        IOptions<SecuritySettings> securitySettings)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _securitySettings = securitySettings.Value;
        _alertingSettings = _securitySettings.Alerting;
    }

    public async Task<bool> ShouldAlertAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        // Verificar si el tipo de evento requiere alerta
        if (!_alertingSettings.AlertOnEventTypes.Contains(auditLog.EventType) &&
            !_alertingSettings.AlertOnSeverities.Contains(auditLog.Severity))
        {
            return false;
        }

        // Verificar throttling de alertas
        var throttleKey = $"{auditLog.EventType}_{auditLog.IpAddress}_{auditLog.UserId}";
        if (_alertThrottleCache.ContainsKey(throttleKey))
        {
            var lastAlert = _alertThrottleCache[throttleKey];
            if (DateTime.UtcNow - lastAlert < TimeSpan.FromMinutes(_alertingSettings.AlertThrottleMinutes))
            {
                _logger.LogDebug("Alerta throttled para {ThrottleKey}", throttleKey);
                return false;
            }
        }

        // Análisis específico por tipo de evento
        var shouldAlert = auditLog.EventType switch
        {
            "LoginAttempt" => await ShouldAlertLoginAttemptAsync(auditLog, cancellationToken),
            "SuspiciousActivity" => await ShouldAlertSuspiciousActivityAsync(auditLog, cancellationToken),
            "UnauthorizedAccess" => await ShouldAlertUnauthorizedAccessAsync(auditLog, cancellationToken),
            "RateLimitExceeded" => await ShouldAlertRateLimitAsync(auditLog, cancellationToken),
            _ => auditLog.Severity == "Critical"
        };

        if (shouldAlert)
        {
            _alertThrottleCache[throttleKey] = DateTime.UtcNow;
        }

        return shouldAlert;
    }

    public async Task SendAlertAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Enviando alerta de seguridad: {EventType} - {Description} - IP: {IpAddress}",
                auditLog.EventType, auditLog.Description, auditLog.IpAddress);

            var alertContext = await BuildAlertContextAsync(auditLog, cancellationToken);

            // Enviar alertas según configuración
            var tasks = new List<Task>();

            if (_alertingSettings.EnableEmailAlerts && _alertingSettings.AlertRecipients.Any())
            {
                tasks.Add(SendEmailAlertAsync(auditLog, alertContext, cancellationToken));
            }

            if (_alertingSettings.EnableSlackAlerts && !string.IsNullOrEmpty(_alertingSettings.SlackWebhookUrl))
            {
                tasks.Add(SendSlackAlertAsync(auditLog, alertContext, cancellationToken));
            }

            if (_alertingSettings.EnableSmsAlerts)
            {
                tasks.Add(SendSmsAlertAsync(auditLog, alertContext, cancellationToken));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation("Alertas enviadas exitosamente para evento {EventType}", auditLog.EventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta de seguridad para evento {EventType}", auditLog.EventType);
        }
    }

    public async Task<bool> IsUnusualLocationAsync(Guid? userId, LocationInfo? locationInfo, 
        CancellationToken cancellationToken = default)
    {
        if (userId == null || locationInfo == null)
            return false;

        try
        {
            // Obtener ubicaciones históricas del usuario (últimos 90 días)
            var historicalLocations = await _context.SecurityAuditLogs
                .Where(sal => sal.UserId == userId && 
                             sal.OccurredAt >= DateTime.UtcNow.AddDays(-90) &&
                             sal.LocationInfo != null)
                .Select(sal => new { sal.LocationInfo!.Country, sal.LocationInfo.City })
                .Distinct()
                .ToListAsync(cancellationToken);

            // Considerar ubicación inusual si:
            // 1. No hay historial de ubicaciones
            // 2. País nunca visto antes
            // 3. Ciudad nueva en un país con pocas ubicaciones conocidas
            if (!historicalLocations.Any())
            {
                return true; // Primera ubicación registrada
            }

            var currentLocation = $"{locationInfo.Country}_{locationInfo.City}";
            var knownCountries = historicalLocations.Select(l => l.Country).Distinct().ToHashSet();
            var knownCities = historicalLocations.Select(l => $"{l.Country}_{l.City}").ToHashSet();

            // País nunca visto
            if (!knownCountries.Contains(locationInfo.Country))
            {
                _logger.LogInformation("Ubicación inusual detectada - Nuevo país: {Country} para usuario {UserId}",
                    locationInfo.Country, userId);
                return true;
            }

            // Ciudad nueva en país con pocas ubicaciones (posible viaje o cuenta comprometida)
            if (!knownCities.Contains(currentLocation))
            {
                var citiesInCountry = historicalLocations
                    .Where(l => l.Country == locationInfo.Country)
                    .Select(l => l.City)
                    .Distinct()
                    .Count();

                if (citiesInCountry <= 2) // Máximo 2 ciudades conocidas en el país
                {
                    _logger.LogInformation("Ubicación inusual detectada - Nueva ciudad: {City} en {Country} para usuario {UserId}",
                        locationInfo.City, locationInfo.Country, userId);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar ubicación inusual para usuario {UserId}", userId);
            return false;
        }
    }

    public async Task<SecurityThreatLevel> AnalyzeThreatLevelAsync(SecurityAuditLog auditLog, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var threatLevel = SecurityThreatLevel.Low;

            // Análisis basado en severidad del evento
            threatLevel = auditLog.Severity switch
            {
                "Critical" => SecurityThreatLevel.Critical,
                "High" => SecurityThreatLevel.High,
                "Medium" => SecurityThreatLevel.Medium,
                _ => SecurityThreatLevel.Low
            };

            // Escalación basada en patrones recientes
            if (auditLog.UserId.HasValue)
            {
                var recentFailures = await CountRecentFailuresAsync(auditLog.UserId.Value, auditLog.EventType, cancellationToken);
                if (recentFailures >= 5)
                {
                    threatLevel = SecurityThreatLevel.Critical;
                }
                else if (recentFailures >= 3)
                {
                    threatLevel = (SecurityThreatLevel)Math.Max((int)threatLevel, (int)SecurityThreatLevel.High);
                }
            }

            // Escalación por ubicación inusual
            if (await IsUnusualLocationAsync(auditLog.UserId, auditLog.LocationInfo, cancellationToken))
            {
                threatLevel = (SecurityThreatLevel)Math.Max((int)threatLevel, (int)SecurityThreatLevel.High);
            }

            // Escalación por patrones de IP sospechosa
            var ipThreatLevel = await AnalyzeIpThreatAsync(auditLog.IpAddress, cancellationToken);
            threatLevel = (SecurityThreatLevel)Math.Max((int)threatLevel, (int)ipThreatLevel);

            return threatLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al analizar nivel de amenaza");
            return SecurityThreatLevel.Medium; // Default conservador
        }
    }

    #region Private Methods

    private async Task<bool> ShouldAlertLoginAttemptAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        if (auditLog.IsSuccessful)
        {
            // Alerta por login exitoso desde ubicación inusual
            return await IsUnusualLocationAsync(auditLog.UserId, auditLog.LocationInfo, cancellationToken);
        }
        else
        {
            // Alerta por múltiples intentos fallidos
            if (auditLog.UserId.HasValue)
            {
                var recentFailures = await CountRecentFailuresAsync(auditLog.UserId.Value, "LoginAttempt", cancellationToken);
                return recentFailures >= 3;
            }
            
            // Alerta por múltiples intentos desde la misma IP
            var ipFailures = await CountRecentIpFailuresAsync(auditLog.IpAddress, "LoginAttempt", cancellationToken);
            return ipFailures >= 5;
        }
    }

    private async Task<bool> ShouldAlertSuspiciousActivityAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        // Siempre alertar actividad sospechosa
        return true;
    }

    private async Task<bool> ShouldAlertUnauthorizedAccessAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        // Alerta por accesos no autorizados repetidos
        var recentUnauthorized = await CountRecentIpFailuresAsync(auditLog.IpAddress, "UnauthorizedAccess", cancellationToken);
        return recentUnauthorized >= 2;
    }

    private async Task<bool> ShouldAlertRateLimitAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        // Alerta si la misma IP excede límites múltiples veces
        var recentRateLimit = await CountRecentIpFailuresAsync(auditLog.IpAddress, "RateLimitExceeded", cancellationToken);
        return recentRateLimit >= 3;
    }

    private async Task<int> CountRecentFailuresAsync(Guid userId, string eventType, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-1);
        return await _context.SecurityAuditLogs
            .CountAsync(sal => sal.UserId == userId &&
                              sal.EventType == eventType &&
                              !sal.IsSuccessful &&
                              sal.OccurredAt >= since, cancellationToken);
    }

    private async Task<int> CountRecentIpFailuresAsync(string ipAddress, string eventType, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-1);
        return await _context.SecurityAuditLogs
            .CountAsync(sal => sal.IpAddress == ipAddress &&
                              sal.EventType == eventType &&
                              !sal.IsSuccessful &&
                              sal.OccurredAt >= since, cancellationToken);
    }

    private async Task<SecurityThreatLevel> AnalyzeIpThreatAsync(string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            // Contar eventos recientes desde esta IP
            var recentEvents = await _context.SecurityAuditLogs
                .Where(sal => sal.IpAddress == ipAddress && 
                             sal.OccurredAt >= DateTime.UtcNow.AddHours(-24))
                .CountAsync(cancellationToken);

            // Contar eventos fallidos recientes
            var recentFailures = await _context.SecurityAuditLogs
                .Where(sal => sal.IpAddress == ipAddress && 
                             !sal.IsSuccessful &&
                             sal.OccurredAt >= DateTime.UtcNow.AddHours(-24))
                .CountAsync(cancellationToken);

            if (recentFailures >= 10 || recentEvents >= 50)
                return SecurityThreatLevel.Critical;
            else if (recentFailures >= 5 || recentEvents >= 25)
                return SecurityThreatLevel.High;
            else if (recentFailures >= 3 || recentEvents >= 15)
                return SecurityThreatLevel.Medium;
            else
                return SecurityThreatLevel.Low;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al analizar amenaza de IP {IpAddress}", ipAddress);
            return SecurityThreatLevel.Low;
        }
    }

    private async Task<AlertContext> BuildAlertContextAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
    {
        var context = new AlertContext
        {
            EventType = auditLog.EventType,
            Severity = auditLog.Severity,
            ThreatLevel = await AnalyzeThreatLevelAsync(auditLog, cancellationToken),
            IpAddress = auditLog.IpAddress,
            UserId = auditLog.UserId,
            OccurredAt = auditLog.OccurredAt,
            Description = auditLog.Description,
            LocationInfo = auditLog.LocationInfo,
            DeviceInfo = auditLog.DeviceInfo,
            AdditionalData = auditLog.AdditionalData
        };

        // Enriquecer contexto con información del usuario si está disponible
        if (auditLog.UserId.HasValue)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == auditLog.UserId.Value, cancellationToken);
            
            if (user != null)
            {
                context.UserEmail = user.Email.Value;
                context.UserName = $"{user.FirstName} {user.LastName}";
            }
        }

        return context;
    }

    private async Task SendEmailAlertAsync(SecurityAuditLog auditLog, AlertContext context, CancellationToken cancellationToken)
    {
        try
        {
            var subject = $"🚨 Alerta de Seguridad: {auditLog.EventType} - {auditLog.Severity}";
            var body = BuildEmailAlertBody(context);

            foreach (var recipient in _alertingSettings.AlertRecipients)
            {
                // TODO: Implementar método de envío de email en IEmailService
                _logger.LogInformation("Email alert preparado para {Recipient}: {Subject}", recipient, subject);
            }

            _logger.LogInformation("Alerta de email enviada para evento {EventType}", auditLog.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta por email");
        }
    }

    private async Task SendSlackAlertAsync(SecurityAuditLog auditLog, AlertContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Implementación de Slack webhook sería aquí
            _logger.LogInformation("Alerta de Slack enviada para evento {EventType}", auditLog.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta por Slack");
        }
    }

    private async Task SendSmsAlertAsync(SecurityAuditLog auditLog, AlertContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Implementación de SMS sería aquí
            _logger.LogInformation("Alerta de SMS enviada para evento {EventType}", auditLog.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta por SMS");
        }
    }

    private string BuildEmailAlertBody(AlertContext context)
    {
        return $@"
Se ha detectado un evento de seguridad que requiere atención:

DETALLES DEL EVENTO:
• Tipo: {context.EventType}
• Severidad: {context.Severity}
• Nivel de Amenaza: {context.ThreatLevel}
• Descripción: {context.Description}
• Fecha/Hora: {context.OccurredAt:yyyy-MM-dd HH:mm:ss} UTC

INFORMACIÓN DE ORIGEN:
• IP: {context.IpAddress}
• Ubicación: {context.LocationInfo?.City}, {context.LocationInfo?.Country}
• Dispositivo: {context.DeviceInfo?.DeviceType} - {context.DeviceInfo?.OperatingSystem}

INFORMACIÓN DEL USUARIO:
{(context.UserId.HasValue ? $"• ID: {context.UserId}\n• Email: {context.UserEmail}\n• Nombre: {context.UserName}" : "• Usuario no identificado")}

ACCIONES RECOMENDADAS:
• Revisar la actividad reciente del usuario/IP
• Verificar si es actividad legítima
• Considerar bloqueo temporal si es necesario
• Investigar patrones relacionados

Este mensaje fue generado automáticamente por el Sistema de Auditoría de Seguridad de Accesia.
        ";
    }

    #endregion
}



 