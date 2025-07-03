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
}
