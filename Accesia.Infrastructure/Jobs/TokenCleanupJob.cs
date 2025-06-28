using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Common.Interfaces;

namespace Accesia.Infrastructure.Jobs;

public class TokenCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(4); // Ejecutar cada 4 horas

    public TokenCleanupJob(IServiceProvider serviceProvider, ILogger<TokenCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Job iniciado. Se ejecutará cada {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Se está cerrando la aplicación
                _logger.LogInformation("Token Cleanup Job detenido por cancellation token");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Token Cleanup Job");
                // Esperar menos tiempo antes del siguiente intento en caso de error
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var cutoffDate = DateTime.UtcNow;
        
        _logger.LogDebug("Iniciando limpieza de tokens expirados antes de {CutoffDate}", cutoffDate);

        try
        {
            // Limpiar tokens de verificación de email expirados
            var emailTokensToClean = await context.Users
                .Where(u => !string.IsNullOrEmpty(u.EmailVerificationToken) && 
                           u.EmailVerificationTokenExpiresAt.HasValue &&
                           u.EmailVerificationTokenExpiresAt.Value < cutoffDate)
                .ToListAsync(cancellationToken);

            foreach (var user in emailTokensToClean)
            {
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiresAt = null;
            }

            // Limpiar tokens de reset de contraseña expirados
            var passwordTokensToClean = await context.Users
                .Where(u => !string.IsNullOrEmpty(u.PasswordResetToken) && 
                           u.PasswordResetTokenExpiresAt.HasValue &&
                           u.PasswordResetTokenExpiresAt.Value < cutoffDate)
                .ToListAsync(cancellationToken);

            foreach (var user in passwordTokensToClean)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiresAt = null;
            }

            // Guardar cambios
            var totalAffected = emailTokensToClean.Count + passwordTokensToClean.Count;
            
            if (totalAffected > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Limpieza completada. Tokens eliminados: {EmailTokens} de verificación, {PasswordTokens} de reset de contraseña", 
                    emailTokensToClean.Count, passwordTokensToClean.Count);
            }
            else
            {
                _logger.LogDebug("No se encontraron tokens expirados para limpiar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de tokens");
            throw; // Re-lanzar para que el método principal pueda manejar el error
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token Cleanup Job deteniéndose...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Token Cleanup Job detenido");
    }
}
