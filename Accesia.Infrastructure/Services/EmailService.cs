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

    public Task SendEmailVerificationAsync(string email, string verificationToken,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar envío real de email usando un proveedor como SendGrid, SES, etc.
        _logger.LogInformation("Enviando email de verificación a {Email} con token {Token}", email, verificationToken);

        // Por ahora solo loggeamos - en producción aquí iría la integración con el proveedor de email
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando email de recuperación de contraseña a {Email} con token {Token}", email,
            resetToken);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando email de bienvenida a {Email} para {FirstName}", email, firstName);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangeNotificationAsync(string email, string firstName, DateTime changedAt,
        string deviceInfo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enviando notificación de cambio de contraseña a {Email} para {FirstName}. Cambio realizado el {ChangedAt} desde dispositivo: {DeviceInfo}",
            email, firstName, changedAt, deviceInfo);

        // TODO: Implementar plantilla de email específica para cambio de contraseña
        // El email debería incluir:
        // - Fecha y hora del cambio
        // - Información del dispositivo/ubicación
        // - Instrucciones de qué hacer si no fue el usuario
        // - Enlaces de contacto/soporte

        return Task.CompletedTask;
    }

    public Task SendEmailChangeVerificationAsync(string newEmail, string firstName, string verificationToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enviando email de verificación de cambio de email a {NewEmail} para {FirstName} con token {Token}",
            newEmail, firstName, verificationToken);

        // TODO: Implementar plantilla de email específica para cambio de email
        // El email debería incluir:
        // - Mensaje de confirmación del cambio de email solicitado
        // - Enlace con el token de verificación
        // - Información de seguridad sobre la solicitud
        // - Instrucciones de qué hacer si no fue el usuario
        // - Tiempo de expiración del token (24 horas)

        return Task.CompletedTask;
    }

    public Task SendAccountDeletionConfirmationEmailAsync(string email, string fullName, string deletionToken,
        DateTime tokenExpiration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enviando email de confirmación de eliminación de cuenta a {Email} para {FullName} con token {Token}. Expira: {Expiration}",
            email, fullName, deletionToken, tokenExpiration);

        // TODO: Implementar plantilla de email específica para confirmación de eliminación
        // El email debería incluir:
        // - Mensaje de confirmación de la solicitud de eliminación
        // - Enlace con el token de confirmación para proceder
        // - Enlace para cancelar la solicitud
        // - Información sobre el período de gracia de 30 días
        // - Tiempo de expiración del token (24 horas)
        // - Advertencias sobre la irreversibilidad de la acción tras el período de gracia

        return Task.CompletedTask;
    }

    public Task SendAccountMarkedForDeletionEmailAsync(string email, string fullName, DateTime permanentDeletionDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enviando email de cuenta marcada para eliminación a {Email} para {FullName}. Eliminación permanente: {PermanentDeletionDate}",
            email, fullName, permanentDeletionDate);

        // TODO: Implementar plantilla de email específica para cuenta marcada para eliminación
        // El email debería incluir:
        // - Confirmación de que la cuenta ha sido marcada para eliminación
        // - Fecha exacta de eliminación permanente
        // - Enlace para cancelar la eliminación durante el período de gracia
        // - Información sobre qué datos serán eliminados
        // - Recordatorio sobre la irreversibilidad tras la fecha límite
        // - Información de contacto en caso de que no haya sido el usuario

        return Task.CompletedTask;
    }

    public Task SendAccountDeletionCancelledEmailAsync(string email, string fullName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando email de cancelación de eliminación a {Email} para {FullName}",
            email, fullName);

        // TODO: Implementar plantilla de email específica para cancelación de eliminación
        // El email debería incluir:
        // - Confirmación de que la eliminación ha sido cancelada
        // - Información sobre el estado actual de la cuenta
        // - Pasos para reactivar la cuenta si es necesario
        // - Recordatorios de seguridad sobre protección de la cuenta
        // - Información de contacto para soporte adicional

        return Task.CompletedTask;
    }
}