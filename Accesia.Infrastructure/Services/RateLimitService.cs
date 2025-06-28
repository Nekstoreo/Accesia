using Accesia.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Services;

public class RateLimitService : IRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitService> _logger;

    // Configuración de rate limits por acción
    private readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
    {
        { "user_registration", new RateLimitConfig { MaxAttempts = 3, WindowMinutes = 60 } },
        { "login", new RateLimitConfig { MaxAttempts = 5, WindowMinutes = 15 } },
        { "password_reset", new RateLimitConfig { MaxAttempts = 3, WindowMinutes = 60 } },
        { "email_verification", new RateLimitConfig { MaxAttempts = 10, WindowMinutes = 60 } },
        { "resend_verification", new RateLimitConfig { MaxAttempts = 3, WindowMinutes = 60 } } // Más restrictivo
    };

    public RateLimitService(IMemoryCache cache, ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> CanPerformActionAsync(string ipAddress, string action, CancellationToken cancellationToken = default)
    {
        if (!_rateLimits.TryGetValue(action, out var config))
        {
            // Si no hay configuración para la acción, permitimos la operación
            return Task.FromResult(true);
        }

        var key = GetCacheKey(ipAddress, action);
        var attempts = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

        // Limpiar intentos fuera de la ventana de tiempo
        var cutoff = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        attempts.RemoveAll(dt => dt < cutoff);

        var canPerform = attempts.Count < config.MaxAttempts;
        
        if (!canPerform)
        {
            _logger.LogWarning("Rate limit excedido para IP {IpAddress} en acción {Action}. Intentos: {Attempts}/{MaxAttempts}", 
                ipAddress, action, attempts.Count, config.MaxAttempts);
        }

        return Task.FromResult(canPerform);
    }

    public Task RecordActionAttemptAsync(string ipAddress, string action, CancellationToken cancellationToken = default)
    {
        if (!_rateLimits.TryGetValue(action, out var config))
            return Task.CompletedTask;

        var key = GetCacheKey(ipAddress, action);
        var attempts = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

        // Limpiar intentos antiguos
        var cutoff = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        attempts.RemoveAll(dt => dt < cutoff);

        // Agregar el intento actual
        attempts.Add(DateTime.UtcNow);

        // Guardar en cache por el tiempo de la ventana + buffer
        var expiration = TimeSpan.FromMinutes(config.WindowMinutes + 5);
        _cache.Set(key, attempts, expiration);

        _logger.LogDebug("Registrado intento de {Action} desde IP {IpAddress}. Total intentos: {Attempts}", 
            action, ipAddress, attempts.Count);

        return Task.CompletedTask;
    }

    public Task<TimeSpan> GetRemainingCooldownAsync(string ipAddress, string action, CancellationToken cancellationToken = default)
    {
        if (!_rateLimits.TryGetValue(action, out var config))
            return Task.FromResult(TimeSpan.Zero);

        var key = GetCacheKey(ipAddress, action);
        var attempts = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

        if (attempts.Count < config.MaxAttempts)
            return Task.FromResult(TimeSpan.Zero);

        // El cooldown termina cuando el intento más antiguo sale de la ventana
        var oldestAttempt = attempts.Min();
        var cooldownEnds = oldestAttempt.AddMinutes(config.WindowMinutes);
        var remaining = cooldownEnds - DateTime.UtcNow;

        return Task.FromResult(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }

    private static string GetCacheKey(string ipAddress, string action)
    {
        return $"rate_limit:{action}:{ipAddress}";
    }

    private class RateLimitConfig
    {
        public int MaxAttempts { get; set; }
        public int WindowMinutes { get; set; }
    }
} 