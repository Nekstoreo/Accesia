using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Entities;

namespace Accesia.Application.Features.Authentication.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChangePasswordHandler> _logger;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IEmailService _emailService;

    public ChangePasswordHandler(
        IApplicationDbContext context,
        ILogger<ChangePasswordHandler> logger,
        IPasswordHashService passwordHashService,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _passwordHashService = passwordHashService;
        _emailService = emailService;
    }

    public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando cambio de contraseña para usuario {UserId}", request.UserId);

        // 1. Buscar usuario con historial de contraseñas
        var user = await _context.Users
            .Include(u => u.PasswordHistories.OrderByDescending(ph => ph.PasswordChangedAt).Take(5))
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario no encontrado: {UserId}", request.UserId);
            throw new UserNotFoundException("Usuario no encontrado", request.UserId.ToString());
        }

        // 2. Verificar contraseña actual
        if (!_passwordHashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Contraseña actual incorrecta para usuario {UserId}", request.UserId);
            throw new CurrentPasswordIncorrectException();
        }

        // 3. Validar nueva contraseña
        var newPassword = new Password(request.NewPassword);
        var newPasswordHash = _passwordHashService.HashPassword(newPassword.Value);

        // 4. Verificar que no sea la misma contraseña actual
        if (_passwordHashService.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("Intento de usar la misma contraseña actual para usuario {UserId}", request.UserId);
            throw new PasswordRecentlyUsedException("La nueva contraseña debe ser diferente a la contraseña actual.");
        }

        // 5. Verificar que la contraseña no fue usada recientemente
        if (user.IsPasswordRecentlyUsed(newPasswordHash))
        {
            _logger.LogWarning("Intento de reutilizar contraseña reciente para usuario {UserId}", request.UserId);
            throw new PasswordRecentlyUsedException();
        }

        try
        {
            // 6. Guardar contraseña anterior en historial
            var passwordHistory = PasswordHistory.Create(user.Id, user.PasswordHash);
            _context.PasswordHistories.Add(passwordHistory);

            // 7. Cambiar contraseña
            user.ChangePassword(newPasswordHash);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Contraseña cambiada exitosamente para usuario {UserId}", request.UserId);

            // 8. Enviar notificación por email
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email.Value, 
                        user.FirstName, 
                        CancellationToken.None);
                    
                    _logger.LogInformation("Email de confirmación de cambio de contraseña enviado a {Email}", user.Email.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de confirmación a {Email}", user.Email.Value);
                }
            }, CancellationToken.None);

            return new ChangePasswordResponse
            {
                Success = true,
                Message = "Contraseña cambiada exitosamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña para usuario {UserId}", request.UserId);
            throw;
        }
    }
} 