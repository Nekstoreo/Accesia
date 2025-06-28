using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IApplicationDbContext context,
        ISessionService sessionService,
        ILogger<LogoutHandler> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Buscar la sesión por token
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Intento de logout con token de sesión inválido desde IP: {IpAddress}", 
                request.IpAddress);
            
            return new LogoutResponse
            {
                Message = "Sesión no encontrada o ya cerrada",
                LogoutAt = DateTime.UtcNow,
                Success = false
            };
        }

        // Verificar que la sesión esté activa
        if (session.Status != SessionStatus.Active)
        {
            _logger.LogWarning("Intento de logout en sesión no activa {SessionToken} para usuario {UserId}", 
                session.SessionToken, session.UserId);
                
            return new LogoutResponse
            {
                Message = "La sesión ya está cerrada",
                LogoutAt = DateTime.UtcNow,
                Success = false
            };
        }

        // Registrar actividad de logout
        _logger.LogInformation("Iniciando logout para usuario {Email} con sesión {SessionToken} desde IP {IpAddress}", 
            session.User.Email.Value, session.SessionToken, request.IpAddress);

        // Invalidar la sesión
        await _sessionService.RevokeSessionAsync(request.SessionToken, cancellationToken);

        // Registrar logout exitoso
        _logger.LogInformation("Logout exitoso para usuario {Email}. Sesión {SessionToken} invalidada", 
            session.User.Email.Value, session.SessionToken);

        return new LogoutResponse
        {
            Message = "Sesión cerrada exitosamente",
            LogoutAt = DateTime.UtcNow,
            Success = true
        };
    }
} 