namespace Accesia.Application.Common.Interfaces;

public interface IRateLimitService
{
    Task<bool> CanPerformActionAsync(string ipAddress, string action, CancellationToken cancellationToken = default);
    Task RecordActionAttemptAsync(string ipAddress, string action, CancellationToken cancellationToken = default);
    Task<TimeSpan> GetRemainingCooldownAsync(string ipAddress, string action, CancellationToken cancellationToken = default);
} 