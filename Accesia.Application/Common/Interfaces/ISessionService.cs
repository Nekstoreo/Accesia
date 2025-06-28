using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Common.Interfaces;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(User user, DeviceInfo deviceInfo, LocationInfo locationInfo, string loginMethod, CancellationToken cancellationToken = default);
    Task<Session?> GetSessionByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<Session?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllUserSessionsExceptCurrentAsync(Guid userId, string currentSessionToken, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task UpdateSessionActivityAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
} 