using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IRateLimitService rateLimitService,
        ILogger<RefreshTokenHandler> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Verificar rate limiting
        if (!await _rateLimitService.CanPerformActionAsync(request.IpAddress, "refresh-token", cancellationToken))
        {
            var cooldown = await _rateLimitService.GetRemainingCooldownAsync(request.IpAddress, "refresh-token", cancellationToken);
            throw new RateLimitExceededException("refresh-token", cooldown);
        }
        
        await _rateLimitService.RecordActionAttemptAsync(request.IpAddress, "refresh-token", cancellationToken);

        // Buscar sesión por refresh token
        var session = await _context.Sessions
            .Include(s => s.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(s => s.RefreshToken == request.RefreshToken, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Intento de refresh con token inválido desde IP: {IpAddress}", request.IpAddress);
            throw new InvalidVerificationTokenException("Refresh token inválido", request.RefreshToken);
        }

        // Verificar si la sesión puede ser renovada
        if (!session.CanBeRefreshed())
        {
            _logger.LogWarning("Intento de refresh con token expirado para usuario {UserId} desde IP: {IpAddress}", 
                session.UserId, request.IpAddress);
            
            // Invalidar sesión expirada
            session.Expire();
            await _context.SaveChangesAsync(cancellationToken);
            
            throw new ExpiredVerificationTokenException("Refresh token expirado", request.RefreshToken);
        }

        // Verificar si el usuario sigue activo
        var user = session.User;
        if (user.Status != UserStatus.Active || user.IsAccountLocked())
        {
            _logger.LogWarning("Intento de refresh para usuario inactivo/bloqueado {UserId} desde IP: {IpAddress}", 
                session.UserId, request.IpAddress);
            
            // Revocar todas las sesiones del usuario
            await _sessionService.RevokeAllUserSessionsAsync(user.Id, cancellationToken);
            
            throw new UserNotFoundException("Usuario no encontrado o inactivo", user.Email.Value);
        }

        // Actualizar actividad de la sesión
        session.UpdateLastActivity();

        // Generar nuevo refresh token
        session.GenerateNewRefreshToken();

        // Generar nuevo access token
        var roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name);
        var permissions = user.GetEffectivePermissions().Select(p => p.Name);
        
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var tokenExpiration = _jwtTokenService.GetTokenExpiration();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token renovado exitosamente para usuario {UserId} desde IP {IpAddress}", 
            user.Id, request.IpAddress);

        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = session.RefreshToken,
            TokenType = "Bearer",
            ExpiresIn = (int)(tokenExpiration - DateTime.UtcNow).TotalSeconds,
            ExpiresAt = tokenExpiration
        };
    }
} 