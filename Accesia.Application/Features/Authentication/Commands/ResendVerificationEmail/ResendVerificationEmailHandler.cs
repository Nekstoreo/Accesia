using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.Entities;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.ResendVerificationEmail;

public class ResendVerificationEmailHandler : IRequestHandler<ResendVerificationEmailCommand, ResendVerificationResponse>
{
    // Constantes para rate limiting y configuración
    private const string RESEND_VERIFICATION_ACTION = "resend_verification";
    private const int MAX_ATTEMPTS_PER_HOUR = 3;
    private const int TOKEN_VALIDITY_HOURS = 24;
    private const int MIN_MINUTES_BETWEEN_RESENDS = 5;

    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResendVerificationEmailHandler> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;

    public ResendVerificationEmailHandler(
        IApplicationDbContext context,
        ILogger<ResendVerificationEmailHandler> logger,
        IRateLimitService rateLimitService,
        IEmailService emailService,
        ITokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _emailService = emailService;
        _tokenService = tokenService;
    }

    public async Task<ResendVerificationResponse> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando proceso de reenvío de verificación para email {Email}", request.Email);

        // 1. Verificar rate limiting estricto
        await CheckRateLimitAsync(request.ClientIpAddress, cancellationToken);

        // 2. Buscar y validar usuario
        var user = await FindUserByEmailAsync(request.Email, cancellationToken);
        
        // 3. Validar que el usuario no esté ya verificado
        if (user.IsEmailVerified)
        {
            _logger.LogWarning("Intento de reenvío para email ya verificado: {Email}", request.Email);
            throw new EmailAlreadyVerifiedException("El email ya está verificado", 
                user.EmailVerificationToken ?? "", request.Email);
        }

        // 4. Verificar si hay un token actual válido (no expirado)
        var currentTokenValid = IsCurrentTokenValid(user);
        var wasTokenRefreshed = false;

        // 5. Generar nuevo token si el actual ha expirado o no existe
        if (!currentTokenValid)
        {
            var newToken = _tokenService.GenerateEmailVerificationToken();
            var tokenExpiration = DateTime.UtcNow.AddHours(TOKEN_VALIDITY_HOURS);
            
            user.SetEmailVerificationToken(newToken, tokenExpiration);
            wasTokenRefreshed = true;
            
            _logger.LogInformation("Token de verificación renovado para email {Email}", request.Email);
        }

        // 6. Guardar cambios si se generó nuevo token
        if (wasTokenRefreshed)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 7. Enviar email de verificación
        await SendVerificationEmailAsync(user, cancellationToken);

        // 8. Registrar intento en rate limiting
        if (request.ClientIpAddress != null)
        {
            await _rateLimitService.RecordActionAttemptAsync(request.ClientIpAddress, RESEND_VERIFICATION_ACTION, cancellationToken);
        }

        // 9. Calcular próximo reenvío permitido
        var nextResendAllowed = DateTime.UtcNow.AddMinutes(MIN_MINUTES_BETWEEN_RESENDS);
        var nextResendAllowedIn = TimeSpan.FromMinutes(MIN_MINUTES_BETWEEN_RESENDS);

        // 10. Retornar respuesta exitosa con información detallada
        return new ResendVerificationResponse
        {
            Success = true,
            Message = wasTokenRefreshed 
                ? "Se ha enviado un nuevo email de verificación con token renovado" 
                : "Se ha reenviado el email de verificación",
            Email = request.Email,
            TokenExpiresAt = user.EmailVerificationTokenExpiresAt,
            WasTokenRefreshed = wasTokenRefreshed,
            NextResendAllowedIn = nextResendAllowedIn,
            NextResendAllowedAt = nextResendAllowed
        };
    }

    private async Task CheckRateLimitAsync(string? clientIpAddress, CancellationToken cancellationToken)
    {
        if (clientIpAddress != null)
        {
            bool canPerformAction = await _rateLimitService.CanPerformActionAsync(clientIpAddress, RESEND_VERIFICATION_ACTION, cancellationToken);
            if (!canPerformAction)
            {
                TimeSpan cooldown = await _rateLimitService.GetRemainingCooldownAsync(clientIpAddress, RESEND_VERIFICATION_ACTION, cancellationToken);
                _logger.LogWarning("Rate limit excedido para reenvío de verificación. IP: {IP}, Cooldown: {Cooldown}", 
                    clientIpAddress, cooldown);
                throw new RateLimitExceededException($"Demasiados intentos de reenvío. Máximo {MAX_ATTEMPTS_PER_HOUR} por hora.", cooldown);
            }
        }
    }

    private async Task<User> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario no encontrado para email: {Email}", email);
            throw new UserNotFoundException("Usuario no encontrado", email);
        }

        return user;
    }

    private bool IsCurrentTokenValid(User user)
    {
        return !string.IsNullOrEmpty(user.EmailVerificationToken) &&
               user.EmailVerificationTokenExpiresAt.HasValue &&
               user.EmailVerificationTokenExpiresAt.Value > DateTime.UtcNow;
    }

    private async Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendEmailVerificationAsync(
                user.Email.Value, 
                user.EmailVerificationToken!, 
                cancellationToken);
            
            _logger.LogInformation("Email de verificación reenviado exitosamente a {Email}", user.Email.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reenviar email de verificación a {Email}", user.Email.Value);
            // No lanzamos la excepción para no interrumpir el flujo, pero loggeamos el error
        }
    }
}
