using MediatR;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    // Constantes para rate limiting
    private const string EMAIL_VERIFICATION_ACTION = "email_verification";
    private const int MAX_ATTEMPTS_PER_HOUR = 10;

    private readonly IApplicationDbContext _context;
    private readonly ILogger<VerifyEmailHandler> _logger;
    private readonly IRateLimitService _rateLimitService;

    public VerifyEmailHandler(
        IApplicationDbContext context,
        ILogger<VerifyEmailHandler> logger,
        IRateLimitService rateLimitService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando proceso de verificación de email para {Email}", request.Email);

        // Verificar rate limiting
        await CheckRateLimitAsync(request.ClientIpAddress, cancellationToken);

        //Buscar usuario por token en la base de datos
        var user = await FindUserByTokenAsync(request.Token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Token de verificación no encontrado: {Token}, Email: {Email}", request.Token, request.Email);
            throw new InvalidVerificationTokenException("Token no encontrado", request.Token, request.Email);
        }

        // Validar token no expirado
        if (await IsTokenExpiredAsync(user, cancellationToken))
        {
            _logger.LogWarning("Token de verificación expirado: {Token}, Email: {Email}", request.Token, request.Email);
            throw new ExpiredVerificationTokenException("Token expirado", request.Token, request.Email);
        }

        // Validar usuario no esté ya verificado
        if (await IsUserAlreadyVerifiedAsync(user, cancellationToken))
        {
            _logger.LogWarning("Email ya verificado: {Email}", request.Email);
            throw new EmailAlreadyVerifiedException("Email ya verificado", request.Token, request.Email);
        }

        // Verificar que el usuario esté en estado PendingConfirmation
        if (user.Status != UserStatus.PendingConfirmation && user.Status != UserStatus.EmailPendingVerification)
        {
            _logger.LogWarning("Estado de usuario incorrecto para verificación de email: {Estado}, Email: {Email}", 
                user.Status, request.Email);
            throw new InvalidOperationException($"El estado del usuario ({user.Status}) no permite la verificación de email");
        }

        //Verificar email
        user.VerifyEmail();

        //Activar cuenta - cambiar estado específicamente de PendingConfirmation a Active
        if (user.Status == UserStatus.PendingConfirmation || user.Status == UserStatus.EmailPendingVerification)
        {
            user.Status = UserStatus.Active;
            _logger.LogInformation("Usuario activado correctamente: {Email}", request.Email);
        }

        //Limpiar token (EmailVerificationToken = null, EmailVerificationTokenExpiresAt = null)
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;

        //Guardar cambios en BD
        await _context.SaveChangesAsync(cancellationToken);

        //Registrar intento en rate limiting
        if (request.ClientIpAddress != null)
        {
            await _rateLimitService.RecordActionAttemptAsync(request.ClientIpAddress, EMAIL_VERIFICATION_ACTION, cancellationToken);
        }

        //Retornar response exitoso
        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verificado correctamente",
            EmailVerifiedAt = user.EmailVerifiedAt,
            IsAccountActivated = user.Status == UserStatus.Active,
            RedirectUrl = "/login?verified=true" // URL opcional para redirección tras verificación
        };
    }

    private async Task CheckRateLimitAsync(string? clientIpAddress, CancellationToken cancellationToken)
    {
        if (clientIpAddress != null)
        {
            bool canPerformAction = await _rateLimitService.CanPerformActionAsync(clientIpAddress, EMAIL_VERIFICATION_ACTION, cancellationToken);
            if (!canPerformAction)
            {
                TimeSpan cooldown = await _rateLimitService.GetRemainingCooldownAsync(clientIpAddress, EMAIL_VERIFICATION_ACTION, cancellationToken);
                _logger.LogWarning("Rate limit excedido para verificación de email. IP: {IP}, Cooldown: {Cooldown}", 
                    clientIpAddress, cooldown);
                throw new RateLimitExceededException($"Demasiados intentos de verificación de email. Máximo {MAX_ATTEMPTS_PER_HOUR} por hora.", cooldown);
            }
        }
    }

    private async Task<User?> FindUserByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);
    }

    private async Task<bool> IsTokenExpiredAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.EmailVerificationTokenExpiresAt.HasValue)
        {
            return true;
        }
        
        return await Task.FromResult(user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow);
    }

    private async Task<bool> IsUserAlreadyVerifiedAsync(User user, CancellationToken cancellationToken)
    {
        return await Task.FromResult(user.IsEmailVerified);
    }
}