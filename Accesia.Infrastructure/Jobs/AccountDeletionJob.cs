using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Jobs;

public class AccountDeletionJob : BackgroundService
{
    private const int GracePeriodDays = 30;
    private readonly ILogger<AccountDeletionJob> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(6); // Ejecutar cada 6 horas
    private readonly IServiceProvider _serviceProvider;

    public AccountDeletionJob(IServiceProvider serviceProvider, ILogger<AccountDeletionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la eliminación permanente de cuentas");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando proceso de eliminación permanente de cuentas");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            // Buscar cuentas marcadas para eliminación que estén fuera del período de gracia
            var cutoffDate = DateTime.UtcNow.AddDays(-GracePeriodDays);

            var accountsToDelete = await context.Users
                .Where(u => u.Status == UserStatus.MarkedForDeletion &&
                            u.MarkedForDeletionAt.HasValue &&
                            u.MarkedForDeletionAt.Value <= cutoffDate)
                .Include(u => u.Sessions)
                .Include(u => u.UserRoles)
                .Include(u => u.PasswordHistories)
                .Include(u => u.AuditLogs)
                .Include(u => u.Settings)
                .ToListAsync(cancellationToken);

            if (!accountsToDelete.Any())
            {
                _logger.LogInformation("No hay cuentas para eliminar permanentemente");
                return;
            }

            _logger.LogInformation("Encontradas {Count} cuentas para eliminación permanente", accountsToDelete.Count);

            foreach (var user in accountsToDelete)
                try
                {
                    _logger.LogInformation("Eliminando permanentemente cuenta del usuario {UserId} ({Email})",
                        user.Id, user.Email.Value);

                    // Enviar email final de notificación antes de eliminar
                    await emailService.SendAccountDeletionCancelledEmailAsync(
                        user.Email.Value,
                        $"{user.FirstName} {user.LastName}",
                        cancellationToken);

                    // Eliminar datos relacionados en orden correcto
                    if (user.Settings != null) context.UserSettings.Remove(user.Settings);

                    // Eliminar relaciones (EF Core debería manejar esto con OnDelete Cascade)
                    context.Users.Remove(user);

                    _logger.LogInformation("Usuario {UserId} eliminado permanentemente", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar permanentemente el usuario {UserId}", user.Id);
                    // Continuar con los demás usuarios incluso si uno falla
                }

            // Guardar todos los cambios
            var deletedCount = await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Eliminación permanente completada. {Count} usuarios eliminados",
                accountsToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el proceso de eliminación permanente de cuentas");
        }
    }
}