using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Authentication.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Authentication.Commands.RequestPasswordReset;

public class RequestPasswordResetHandler : IRequestHandler<RequestPasswordResetCommand, RequestPasswordResetResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestPasswordResetHandler> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly ITokenService _tokenService;

    public RequestPasswordResetHandler(
        IApplicationDbContext context,
        ILogger<RequestPasswordResetHandler> logger,
        IEmailService emailService,
        IRateLimitService rateLimitService,
        ITokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _rateLimitService = rateLimitService;
        _tokenService = tokenService;
    }

    public async Task<RequestPasswordResetResponse> Handle(RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Solicitud de restablecimiento de contraseña para email {Email}", request.Email);

        // 1. Verificar rate limiting por IP
        await CheckRateLimitAsync(request.ClientIpAddress);

        // 2. Buscar usuario (sin revelar si existe o no)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email, cancellationToken);

        // 3. Si el usuario existe, generar token y enviar email
        if (user != null)
        {
            // Generar token de restablecimiento
            var resetToken = _tokenService.GeneratePasswordResetToken();
            var tokenExpiration = DateTime.UtcNow.AddHours(1); // Token válido por 1 hora

            // Establecer token en el usuario
            user.SetPasswordResetToken(resetToken, tokenExpiration);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Token de restablecimiento generado para usuario {UserId}", user.Id);

                // Enviar email de restablecimiento de forma asíncrona
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendPasswordResetAsync(
                            user.Email.Value,
                            resetToken,
                            CancellationToken.None);

                        _logger.LogInformation("Email de restablecimiento enviado a {Email}", request.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar email de restablecimiento a {Email}", request.Email);
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar token de restablecimiento para email {Email}", request.Email);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Solicitud de restablecimiento para email inexistente: {Email}", request.Email);
        }

        // 4. Registrar el intento en rate limiting
        if (!string.IsNullOrEmpty(request.ClientIpAddress))
            await _rateLimitService.RecordActionAttemptAsync(
                request.ClientIpAddress,
                "password_reset_request");

        // 5. Siempre retornar la misma respuesta genérica (no revelar si el usuario existe)
        return new RequestPasswordResetResponse
        {
            Success = true,
            Message =
                "Si el email está registrado en nuestro sistema, recibirás instrucciones de restablecimiento de contraseña."
        };
    }

    private async Task CheckRateLimitAsync(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return;

        var canRequest = await _rateLimitService.CanPerformActionAsync(ipAddress, "password_reset_request");

        if (!canRequest)
        {
            var cooldown = await _rateLimitService.GetRemainingCooldownAsync(ipAddress, "password_reset_request");
            _logger.LogWarning("Solicitud de restablecimiento bloqueada por rate limit desde IP {IpAddress}",
                ipAddress);
            throw new RateLimitExceededException(cooldown);
        }
    }
}