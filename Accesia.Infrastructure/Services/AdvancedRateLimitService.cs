using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accesia.Infrastructure.Services;

public class AdvancedRateLimitService : IAdvancedRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly ILogger<AdvancedRateLimitService> _logger;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly SecuritySettings _securitySettings;

    public AdvancedRateLimitService(
        IMemoryCache cache,
        ILogger<AdvancedRateLimitService> logger,
        ISecurityAuditService securityAuditService,
        IDeviceInfoService deviceInfoService,
        IOptions<SecuritySettings> securitySettings)
    {
        _cache = cache;
        _logger = logger;
        _securityAuditService = securityAuditService;
        _deviceInfoService = deviceInfoService;
        _securitySettings = securitySettings.Value;
    }

    public async Task<bool> CanPerformActionAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var policy = GetPolicy(actionKey);
        if (policy == null) return true;

        var keys = GenerateKeys(actionKey, ipAddress, userId, endpoint);

        // Verificar bloqueos manuales primero
        foreach (var key in keys)
            if (await IsBlockedAsync(key, cancellationToken))
                return false;

        // Verificar límites por cada clave (IP, Usuario, Endpoint)
        foreach (var key in keys)
        {
            var canProceed = await CheckLimitForKeyAsync(key, policy, cancellationToken);
            if (!canProceed) return false;
        }

        return true;
    }

    public async Task RecordActionAttemptAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var policy = GetPolicy(actionKey);
        if (policy == null) return;

        var keys = GenerateKeys(actionKey, ipAddress, userId, endpoint);

        foreach (var key in keys) await RecordAttemptForKeyAsync(key, policy, cancellationToken);

        _logger.LogDebug("Registrado intento de {ActionKey} - IP: {IpAddress}, Usuario: {UserId}, Endpoint: {Endpoint}",
            actionKey, ipAddress, userId, endpoint);
    }

    public async Task<TimeSpan> GetRemainingCooldownAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var policy = GetPolicy(actionKey);
        if (policy == null) return TimeSpan.Zero;

        var keys = GenerateKeys(actionKey, ipAddress, userId, endpoint);
        var maxCooldown = TimeSpan.Zero;

        foreach (var key in keys)
        {
            var cooldown = await GetCooldownForKeyAsync(key, policy, cancellationToken);
            if (cooldown > maxCooldown)
                maxCooldown = cooldown;
        }

        return maxCooldown;
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var policy = GetPolicy(actionKey);
        if (policy == null)
            return new RateLimitStatus
            {
                ActionKey = actionKey,
                CanProceed = true,
                LimiterType = "None"
            };

        var key = GeneratePrimaryKey(actionKey, ipAddress, userId);
        var attempts = GetAttemptsForKey(key, policy);
        var canProceed = await CanPerformActionAsync(actionKey, ipAddress, userId, endpoint, cancellationToken);
        var cooldown = await GetRemainingCooldownAsync(actionKey, ipAddress, userId, endpoint, cancellationToken);
        var isBlocked = await IsBlockedAsync(key, cancellationToken);

        return new RateLimitStatus
        {
            ActionKey = actionKey,
            LimiterType = policy.Type,
            CanProceed = canProceed,
            RemainingAttempts = Math.Max(0, policy.MaxAttempts - attempts.Count),
            MaxAttempts = policy.MaxAttempts,
            WindowDuration = TimeSpan.FromMinutes(policy.WindowMinutes),
            CooldownRemaining = cooldown > TimeSpan.Zero ? cooldown : null,
            WindowStart = GetWindowStart(policy),
            WindowEnd = GetWindowEnd(policy),
            IsBlocked = isBlocked,
            BlockReason = isBlocked ? GetBlockReason(key) : null
        };
    }

    public async Task BlockKeyAsync(string key, TimeSpan duration, string reason,
        CancellationToken cancellationToken = default)
    {
        var blockInfo = new BlockInfo
        {
            Reason = reason,
            BlockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        };

        _cache.Set($"block:{key}", blockInfo, duration);

        _logger.LogWarning("Clave bloqueada manualmente: {Key}, Duración: {Duration}, Razón: {Reason}",
            key, duration, reason);

        await Task.CompletedTask;
    }

    public async Task UnblockKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove($"block:{key}");

        _logger.LogInformation("Clave desbloqueada manualmente: {Key}", key);

        await Task.CompletedTask;
    }

    public async Task<bool> IsBlockedAsync(string key, CancellationToken cancellationToken = default)
    {
        var blockInfo = _cache.Get<BlockInfo>($"block:{key}");
        if (blockInfo == null) return false;

        if (DateTime.UtcNow > blockInfo.ExpiresAt)
        {
            _cache.Remove($"block:{key}");
            return false;
        }

        return true;
    }

    public async Task<Dictionary<string, int>> GetViolationStatisticsAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // Esto requeriría un almacenamiento persistente de violaciones
        // Por ahora retornamos estadísticas básicas del cache
        await Task.CompletedTask;
        return new Dictionary<string, int>();
    }

    public async Task ResetLimitAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var keys = GenerateKeys(actionKey, ipAddress, userId, endpoint);

        foreach (var key in keys) _cache.Remove(key);

        _logger.LogInformation("Rate limit reseteado para {ActionKey} - IP: {IpAddress}, Usuario: {UserId}",
            actionKey, ipAddress, userId);

        await Task.CompletedTask;
    }

    public async Task ConfigurePolicyAsync(string actionKey, RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        _securitySettings.RateLimit.Policies[actionKey] = policy;

        _logger.LogInformation(
            "Política de rate limit configurada para {ActionKey}: {MaxAttempts} intentos en {WindowMinutes} minutos",
            actionKey, policy.MaxAttempts, policy.WindowMinutes);

        await Task.CompletedTask;
    }

    private RateLimitPolicy? GetPolicy(string actionKey)
    {
        return _securitySettings.RateLimit.Policies.GetValueOrDefault(actionKey);
    }

    private List<string> GenerateKeys(string actionKey, string ipAddress, Guid? userId, string? endpoint)
    {
        var keys = new List<string>();

        if (_securitySettings.RateLimit.EnableIpSpecificLimits) keys.Add($"rate_limit:{actionKey}:ip:{ipAddress}");

        if (_securitySettings.RateLimit.EnableUserSpecificLimits && userId.HasValue)
            keys.Add($"rate_limit:{actionKey}:user:{userId.Value}");

        if (_securitySettings.RateLimit.EnableEndpointSpecificLimits && !string.IsNullOrEmpty(endpoint))
            keys.Add($"rate_limit:{actionKey}:endpoint:{endpoint.Replace("/", "_")}");

        // Siempre incluir clave combinada como respaldo
        keys.Add(GeneratePrimaryKey(actionKey, ipAddress, userId));

        return keys.Distinct().ToList();
    }

    private string GeneratePrimaryKey(string actionKey, string ipAddress, Guid? userId)
    {
        var userPart = userId?.ToString() ?? "anonymous";
        return $"rate_limit:{actionKey}:combined:{ipAddress}:{userPart}";
    }

    private async Task<bool> CheckLimitForKeyAsync(string key, RateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        switch (policy.Type.ToLowerInvariant())
        {
            case "fixedwindow":
                return CheckFixedWindow(key, policy);
            case "slidingwindow":
                return CheckSlidingWindow(key, policy);
            case "tokenbucket":
                return CheckTokenBucket(key, policy);
            default:
                return CheckFixedWindow(key, policy); // Fallback
        }
    }

    private async Task RecordAttemptForKeyAsync(string key, RateLimitPolicy policy, CancellationToken cancellationToken)
    {
        switch (policy.Type.ToLowerInvariant())
        {
            case "fixedwindow":
                RecordFixedWindowAttempt(key, policy);
                break;
            case "slidingwindow":
                RecordSlidingWindowAttempt(key, policy);
                break;
            case "tokenbucket":
                RecordTokenBucketAttempt(key, policy);
                break;
            default:
                RecordFixedWindowAttempt(key, policy);
                break;
        }
    }

    private async Task<TimeSpan> GetCooldownForKeyAsync(string key, RateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        var attempts = GetAttemptsForKey(key, policy);
        if (attempts.Count < policy.MaxAttempts) return TimeSpan.Zero;

        switch (policy.Type.ToLowerInvariant())
        {
            case "fixedwindow":
                var windowStart = GetWindowStart(policy);
                var windowEnd = windowStart.AddMinutes(policy.WindowMinutes);
                return windowEnd > DateTime.UtcNow ? windowEnd - DateTime.UtcNow : TimeSpan.Zero;

            case "slidingwindow":
            case "tokenbucket":
                var oldestAttempt = attempts.Min();
                var cooldownEnd = oldestAttempt.AddMinutes(policy.WindowMinutes);
                return cooldownEnd > DateTime.UtcNow ? cooldownEnd - DateTime.UtcNow : TimeSpan.Zero;

            default:
                return TimeSpan.Zero;
        }
    }

    private bool CheckFixedWindow(string key, RateLimitPolicy policy)
    {
        var attempts = GetAttemptsForKey(key, policy);
        var windowStart = GetWindowStart(policy);

        // Filtrar intentos dentro de la ventana actual
        var attemptsInWindow = attempts.Where(a => a >= windowStart).ToList();
        return attemptsInWindow.Count < policy.MaxAttempts;
    }

    private bool CheckSlidingWindow(string key, RateLimitPolicy policy)
    {
        var attempts = GetAttemptsForKey(key, policy);
        var cutoff = DateTime.UtcNow.AddMinutes(-policy.WindowMinutes);

        var validAttempts = attempts.Where(a => a >= cutoff).ToList();
        return validAttempts.Count < policy.MaxAttempts;
    }

    private bool CheckTokenBucket(string key, RateLimitPolicy policy)
    {
        var bucketKey = $"bucket:{key}";
        var bucket = _cache.Get<TokenBucket>(bucketKey) ?? new TokenBucket
        {
            Tokens = policy.TokensPerPeriod,
            LastRefill = DateTime.UtcNow
        };

        // Rellenar tokens basado en el tiempo transcurrido
        var elapsed = DateTime.UtcNow - bucket.LastRefill;
        var periodsElapsed = elapsed.TotalMinutes / policy.ReplenishmentPeriodMinutes;
        var tokensToAdd = (int)(periodsElapsed * policy.TokensPerPeriod);

        if (tokensToAdd > 0)
        {
            bucket.Tokens = Math.Min(policy.TokensPerPeriod, bucket.Tokens + tokensToAdd);
            bucket.LastRefill = DateTime.UtcNow;
        }

        var canProceed = bucket.Tokens > 0;

        // Actualizar bucket en cache
        var expiration = TimeSpan.FromMinutes(policy.ReplenishmentPeriodMinutes * 2);
        _cache.Set(bucketKey, bucket, expiration);

        return canProceed;
    }

    private void RecordFixedWindowAttempt(string key, RateLimitPolicy policy)
    {
        var attempts = GetAttemptsForKey(key, policy);
        var windowStart = GetWindowStart(policy);

        // Limpiar intentos fuera de la ventana actual
        attempts.RemoveAll(a => a < windowStart);
        attempts.Add(DateTime.UtcNow);

        var expiration = TimeSpan.FromMinutes(policy.WindowMinutes + 5);
        _cache.Set(key, attempts, expiration);
    }

    private void RecordSlidingWindowAttempt(string key, RateLimitPolicy policy)
    {
        var attempts = GetAttemptsForKey(key, policy);
        var cutoff = DateTime.UtcNow.AddMinutes(-policy.WindowMinutes);

        attempts.RemoveAll(a => a < cutoff);
        attempts.Add(DateTime.UtcNow);

        var expiration = TimeSpan.FromMinutes(policy.WindowMinutes + 5);
        _cache.Set(key, attempts, expiration);
    }

    private void RecordTokenBucketAttempt(string key, RateLimitPolicy policy)
    {
        var bucketKey = $"bucket:{key}";
        var bucket = _cache.Get<TokenBucket>(bucketKey) ?? new TokenBucket
        {
            Tokens = policy.TokensPerPeriod,
            LastRefill = DateTime.UtcNow
        };

        if (bucket.Tokens > 0) bucket.Tokens--;

        var expiration = TimeSpan.FromMinutes(policy.ReplenishmentPeriodMinutes * 2);
        _cache.Set(bucketKey, bucket, expiration);

        // También registrar el intento para estadísticas
        var attempts = GetAttemptsForKey(key, policy);
        attempts.Add(DateTime.UtcNow);
        _cache.Set(key, attempts, expiration);
    }

    private List<DateTime> GetAttemptsForKey(string key, RateLimitPolicy policy)
    {
        return _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();
    }

    private DateTime GetWindowStart(RateLimitPolicy policy)
    {
        var now = DateTime.UtcNow;
        var windowMinutes = policy.WindowMinutes;

        // Calcular inicio de ventana fija
        var totalMinutes = (int)now.TimeOfDay.TotalMinutes;
        var windowsElapsed = totalMinutes / windowMinutes;
        var windowStartMinutes = windowsElapsed * windowMinutes;

        return now.Date.AddMinutes(windowStartMinutes);
    }

    private DateTime GetWindowEnd(RateLimitPolicy policy)
    {
        return GetWindowStart(policy).AddMinutes(policy.WindowMinutes);
    }

    private string? GetBlockReason(string key)
    {
        var blockInfo = _cache.Get<BlockInfo>($"block:{key}");
        return blockInfo?.Reason;
    }

    private class BlockInfo
    {
        public string Reason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private class TokenBucket
    {
        public int Tokens { get; set; }
        public DateTime LastRefill { get; set; }
    }
}