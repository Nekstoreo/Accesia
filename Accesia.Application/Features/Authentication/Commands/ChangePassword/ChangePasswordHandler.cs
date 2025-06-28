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
        _logger.LogInformation("Iniciando cambio de contraseña para usuario {UserId} desde IP {ClientIp}", 
            request.UserId, request.ClientIp);

        // 1. Buscar usuario con historial de contraseñas
        var user = await _context.Users
            .Include(u => u.PasswordHistories.OrderByDescending(ph => ph.PasswordChangedAt).Take(5))
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario no encontrado para cambio de contraseña: {UserId} desde IP {ClientIp}", 
                request.UserId, request.ClientIp);
            throw new UserNotFoundException("Usuario no encontrado", request.UserId.ToString());
        }

        // 2. Verificar contraseña actual
        if (!_passwordHashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Contraseña actual incorrecta para usuario {UserId} ({Email}) desde IP {ClientIp}", 
                request.UserId, user.Email.Value, request.ClientIp);
            throw new CurrentPasswordIncorrectException();
        }

        // 3. Validar nueva contraseña
        var newPassword = new Password(request.NewPassword);
        var newPasswordHash = _passwordHashService.HashPassword(newPassword.Value);

        // 4. Verificar que no sea la misma contraseña actual
        if (_passwordHashService.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("Intento de usar la misma contraseña actual para usuario {UserId} ({Email}) desde IP {ClientIp}", 
                request.UserId, user.Email.Value, request.ClientIp);
            throw new PasswordRecentlyUsedException("La nueva contraseña debe ser diferente a la contraseña actual.");
        }

        // 5. Verificar que la contraseña no fue usada recientemente
        if (user.IsPasswordRecentlyUsed(newPasswordHash))
        {
            _logger.LogWarning("Intento de reutilizar contraseña reciente para usuario {UserId} ({Email}) desde IP {ClientIp}", 
                request.UserId, user.Email.Value, request.ClientIp);
            throw new PasswordRecentlyUsedException();
        }

        try
        {
            // 6. Guardar contraseña anterior en historial
            var passwordHistory = PasswordHistory.Create(user.Id, user.PasswordHash);
            _context.PasswordHistories.Add(passwordHistory);

            // 7. Cambiar contraseña
            var changeTimestamp = DateTime.UtcNow;
            user.ChangePassword(newPasswordHash);

            await _context.SaveChangesAsync(cancellationToken);

            // Log de auditoría detallado
            _logger.LogInformation("Contraseña cambiada exitosamente para usuario {UserId} ({Email}) el {Timestamp}. " +
                "Cambio realizado desde IP {ClientIp} con User-Agent: {UserAgent}",
                request.UserId, user.Email.Value, changeTimestamp, request.ClientIp, request.UserAgent);

            // 8. Enviar notificación por email
            _ = Task.Run(async () =>
            {
                try
                {
                    var deviceInfo = $"IP: {request.ClientIp}, Navegador: {request.UserAgent}";
                    await _emailService.SendPasswordChangeNotificationAsync(
                        user.Email.Value, 
                        user.FirstName, 
                        changeTimestamp,
                        deviceInfo,
                        CancellationToken.None);
                    
                    _logger.LogInformation("Notificación de cambio de contraseña enviada a {Email} para usuario {UserId}", 
                        user.Email.Value, request.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar notificación de cambio de contraseña a {Email} para usuario {UserId}", 
                        user.Email.Value, request.UserId);
                }
            }, CancellationToken.None);

            return new ChangePasswordResponse
            {
                Success = true,
                Message = "Contraseña cambiada exitosamente. Se ha enviado una confirmación a tu email."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña para usuario {UserId} ({Email}) desde IP {ClientIp}", 
                request.UserId, user.Email.Value, request.ClientIp);
            throw;
        }
    }
} 