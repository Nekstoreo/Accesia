using Accesia.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        // TODO: Implementar envío real de email usando un proveedor como SendGrid, SES, etc.
        _logger.LogInformation("Enviando email de verificación a {Email} con token {Token}", email, verificationToken);
        
        // Por ahora solo loggeamos - en producción aquí iría la integración con el proveedor de email
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando email de recuperación de contraseña a {Email} con token {Token}", email, resetToken);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando email de bienvenida a {Email} para {FirstName}", email, firstName);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangeNotificationAsync(string email, string firstName, DateTime changedAt, string deviceInfo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando notificación de cambio de contraseña a {Email} para {FirstName}. Cambio realizado el {ChangedAt} desde dispositivo: {DeviceInfo}", 
            email, firstName, changedAt, deviceInfo);
        
        // TODO: Implementar plantilla de email específica para cambio de contraseña
        // El email debería incluir:
        // - Fecha y hora del cambio
        // - Información del dispositivo/ubicación
        // - Instrucciones de qué hacer si no fue el usuario
        // - Enlaces de contacto/soporte
        
        return Task.CompletedTask;
    }
} 