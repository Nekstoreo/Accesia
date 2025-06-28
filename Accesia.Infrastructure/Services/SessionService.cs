using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;

namespace Accesia.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(IApplicationDbContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Session> CreateSessionAsync(User user, DeviceInfo deviceInfo, LocationInfo locationInfo, string loginMethod, CancellationToken cancellationToken = default)
    {
        // Verificar si es un dispositivo conocido
        var isKnownDevice = await IsKnownDeviceAsync(user.Id, deviceInfo, cancellationToken);

        var session = new Session
        {
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            Status = SessionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            LastActivityAt = DateTime.UtcNow,
            DeviceInfo = deviceInfo,
            LocationInfo = locationInfo,
            IsKnownDevice = isKnownDevice,
            DeviceName = null, // Se puede proporcionar por separado
            LoginMethod = ParseLoginMethod(loginMethod),
            MfaVerified = false,
            TwoFactorRequired = false,
            RiskScore = CalculateRiskScore(user, deviceInfo, locationInfo, isKnownDevice),
            UserAgent = deviceInfo.UserAgent,
            InitialIpAddress = locationInfo.IpAddress,
            LastIpAddress = locationInfo.IpAddress
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sesión creada para usuario {UserId} con token {SessionToken}", 
            user.Id, session.SessionToken);

        return session;
    }

    public async Task<Session?> GetSessionByTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);
    }

    public async Task<Session?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);

        if (session?.CanBeRefreshed() == true)
        {
            session.GenerateNewRefreshToken();
            session.UpdateLastActivity();
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Sesión renovada para usuario {UserId}", session.UserId);
            return session;
        }

        return null;
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);

        if (session != null)
        {
            session.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Sesión revocada: {SessionToken} para usuario {UserId}", 
                sessionToken, session.UserId);
        }
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        if (sessions.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Todas las sesiones revocadas para usuario {UserId}. Total: {Count}", 
                userId, sessions.Count);
        }
    }

    public async Task RevokeAllUserSessionsExceptCurrentAsync(Guid userId, string currentSessionToken, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && 
                       s.Status == SessionStatus.Active && 
                       s.SessionToken != currentSessionToken)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        if (sessions.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sesiones adicionales revocadas para usuario {UserId}. Total: {Count}", 
                userId, sessions.Count);
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);

        return session?.IsActive() == true;
    }

    public async Task UpdateSessionActivityAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);

        if (session?.IsActive() == true)
        {
            session.UpdateLastActivity();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessions = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Active && 
                       (s.ExpiresAt < DateTime.UtcNow || s.RefreshTokenExpiresAt < DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        foreach (var session in expiredSessions)
        {
            session.Expire();
        }

        if (expiredSessions.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sesiones expiradas limpiadas. Total: {Count}", expiredSessions.Count);
        }
    }

    private async Task<bool> IsKnownDeviceAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        return await _context.Sessions
            .AnyAsync(s => s.UserId == userId && 
                          s.DeviceInfo.DeviceFingerprint == deviceInfo.DeviceFingerprint &&
                          s.Status == SessionStatus.Active, 
                      cancellationToken);
    }

    private static LoginMethod ParseLoginMethod(string loginMethod)
    {
        return loginMethod.ToUpperInvariant() switch
        {
            "PASSWORD" => LoginMethod.Password,
            "GOOGLE" => LoginMethod.GoogleOAuth,
            "MICROSOFT" => LoginMethod.MicrosoftOAuth,
            "GITHUB" => LoginMethod.GitHubOAuth,
            "SAML" => LoginMethod.SAML,
            "MFA" => LoginMethod.MFA,
            "APIKEY" => LoginMethod.ApiKey,
            _ => LoginMethod.Password
        };
    }

    private static int CalculateRiskScore(User user, DeviceInfo deviceInfo, LocationInfo locationInfo, bool isKnownDevice)
    {
        var riskScore = 0;

        // Incrementar riesgo si no es dispositivo conocido
        if (!isKnownDevice)
            riskScore += 20;

        // Incrementar riesgo si hay intentos fallidos recientes
        if (user.FailedLoginAttempts > 0)
            riskScore += user.FailedLoginAttempts * 5;

        // Incrementar riesgo por otros factores
        // (el nombre de dispositivo se maneja a nivel de sesión)

        return Math.Min(riskScore, 100); // Máximo 100
    }
} 