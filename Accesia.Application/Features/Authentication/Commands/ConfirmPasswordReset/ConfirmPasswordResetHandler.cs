using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Authentication.Commands.ConfirmPasswordReset;

public class ConfirmPasswordResetHandler : IRequestHandler<ConfirmPasswordResetCommand, ConfirmPasswordResetResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmPasswordResetHandler> _logger;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ISessionService _sessionService;

    public ConfirmPasswordResetHandler(
        IApplicationDbContext context,
        ILogger<ConfirmPasswordResetHandler> logger,
        IPasswordHashService passwordHashService,
        IEmailService emailService,
        ISessionService sessionService)
    {
        _context = context;
        _logger = logger;
        _passwordHashService = passwordHashService;
        _emailService = emailService;
        _sessionService = sessionService;
    }

    public async Task<ConfirmPasswordResetResponse> Handle(ConfirmPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirmando restablecimiento de contraseña con token {Token}", request.Token);

        // 1. Buscar usuario por token
        var user = await _context.Users
            .Include(u => u.PasswordHistories.OrderByDescending(ph => ph.PasswordChangedAt).Take(5))
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Token de restablecimiento no encontrado: {Token}", request.Token);
            throw new InvalidPasswordResetTokenException();
        }

        // 2. Validar que el token no haya expirado
        if (!user.IsPasswordResetTokenValid(request.Token))
        {
            _logger.LogWarning("Token de restablecimiento expirado para usuario {UserId}", user.Id);
            throw new InvalidPasswordResetTokenException();
        }

        // 3. Validar nueva contraseña
        var newPassword = new Password(request.NewPassword);
        var newPasswordHash = _passwordHashService.HashPassword(newPassword.Value);

        // 4. Verificar que la contraseña no fue usada recientemente
        if (user.IsPasswordRecentlyUsed(newPasswordHash))
        {
            _logger.LogWarning("Intento de reutilizar contraseña reciente para usuario {UserId}", user.Id);
            throw new PasswordRecentlyUsedException();
        }

        try
        {
            // 5. Guardar contraseña anterior en historial
            var passwordHistory = PasswordHistory.Create(user.Id, user.PasswordHash);
            _context.PasswordHistories.Add(passwordHistory);

            // 6. Cambiar contraseña
            user.ChangePassword(newPasswordHash);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Contraseña restablecida exitosamente para usuario {UserId}", user.Id);

            // 7. Invalidar todas las sesiones activas del usuario
            await _sessionService.RevokeAllUserSessionsAsync(user.Id, cancellationToken);

            _logger.LogInformation("Todas las sesiones invalidadas para usuario {UserId}", user.Id);

            // 8. Enviar notificación por email
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email.Value,
                        user.FirstName,
                        CancellationToken.None);

                    _logger.LogInformation("Email de confirmación de cambio de contraseña enviado a {Email}",
                        user.Email.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de confirmación a {Email}", user.Email.Value);
                }
            }, CancellationToken.None);

            return new ConfirmPasswordResetResponse
            {
                Success = true,
                Message = "Contraseña restablecida exitosamente. Por favor, inicia sesión con tu nueva contraseña."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al restablecer contraseña para usuario {UserId}", user.Id);
            throw;
        }
    }
}