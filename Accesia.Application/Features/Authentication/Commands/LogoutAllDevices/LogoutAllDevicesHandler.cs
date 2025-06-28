using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.LogoutAllDevices;

public class LogoutAllDevicesHandler : IRequestHandler<LogoutAllDevicesCommand, LogoutAllDevicesResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<LogoutAllDevicesHandler> _logger;

    public LogoutAllDevicesHandler(
        IApplicationDbContext context,
        ISessionService sessionService,
        ILogger<LogoutAllDevicesHandler> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<LogoutAllDevicesResponse> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
    {
        // Buscar la sesión actual para obtener el userId
        var currentSession = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == request.CurrentSessionToken, cancellationToken);

        if (currentSession == null)
        {
            _logger.LogWarning("Intento de logout de todos los dispositivos con token de sesión inválido desde IP: {IpAddress}", 
                request.IpAddress);
            
            return new LogoutAllDevicesResponse
            {
                Message = "Sesión actual no encontrada",
                LogoutAt = DateTime.UtcNow,
                SessionsTerminated = 0,
                Success = false
            };
        }

        if (currentSession.Status != SessionStatus.Active)
        {
            _logger.LogWarning("Intento de logout de todos los dispositivos con sesión no activa {SessionToken} para usuario {UserId}", 
                currentSession.SessionToken, currentSession.UserId);
                
            return new LogoutAllDevicesResponse
            {
                Message = "La sesión actual no está activa",
                LogoutAt = DateTime.UtcNow,
                SessionsTerminated = 0,
                Success = false
            };
        }

        // Contar sesiones activas antes del logout
        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == currentSession.UserId && s.Status == SessionStatus.Active)
            .CountAsync(cancellationToken);

        _logger.LogInformation("Iniciando logout de todos los dispositivos para usuario {Email}. Sesiones activas: {ActiveSessions}", 
            currentSession.User.Email.Value, activeSessions);

        // Invalidar todas las sesiones del usuario
        await _sessionService.RevokeAllUserSessionsAsync(currentSession.UserId, cancellationToken);

        _logger.LogInformation("Logout de todos los dispositivos completado para usuario {Email}. {SessionsTerminated} sesiones terminadas", 
            currentSession.User.Email.Value, activeSessions);

        return new LogoutAllDevicesResponse
        {
            Message = "Todas las sesiones han sido cerradas exitosamente",
            LogoutAt = DateTime.UtcNow,
            SessionsTerminated = activeSessions,
            Success = true
        };
    }
} 