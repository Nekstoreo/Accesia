using Accesia.Application.Common.Settings;

namespace Accesia.Application.Common.Interfaces;

public interface IAdvancedRateLimitService
{
    Task<bool> CanPerformActionAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default);

    Task RecordActionAttemptAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default);

    Task<TimeSpan> GetRemainingCooldownAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default);

    Task<RateLimitStatus> GetRateLimitStatusAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default);

    Task BlockKeyAsync(string key, TimeSpan duration, string reason, CancellationToken cancellationToken = default);

    Task UnblockKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> IsBlockedAsync(string key, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetViolationStatisticsAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    Task ResetLimitAsync(string actionKey, string ipAddress, Guid? userId = null,
        string? endpoint = null, CancellationToken cancellationToken = default);

    Task ConfigurePolicyAsync(string actionKey, RateLimitPolicy policy, CancellationToken cancellationToken = default);
}

public class RateLimitStatus
{
    public string ActionKey { get; set; } = string.Empty;
    public string LimiterType { get; set; } = string.Empty;
    public bool CanProceed { get; set; }
    public int RemainingAttempts { get; set; }
    public int MaxAttempts { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public TimeSpan? CooldownRemaining { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}