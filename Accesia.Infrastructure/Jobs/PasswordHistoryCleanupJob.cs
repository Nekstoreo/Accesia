using Accesia.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Jobs;

public class PasswordHistoryCleanupJob
{
    private readonly ILogger<PasswordHistoryCleanupJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PasswordHistoryCleanupJob(
        ILogger<PasswordHistoryCleanupJob> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando limpieza de historial de contraseñas");

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Mantener solo las últimas 10 contraseñas por usuario
            // y eliminar las más antiguas de 1 año
            var cutoffDate = DateTime.UtcNow.AddYears(-1);
            const int maxPasswordHistoryPerUser = 10;

            // Obtener todos los usuarios con su historial de contraseñas
            var usersWithTooManyPasswords = await context.Users
                .Include(u => u.PasswordHistories)
                .Where(u => u.PasswordHistories.Count > maxPasswordHistoryPerUser)
                .ToListAsync(cancellationToken);

            var totalRemoved = 0;

            foreach (var user in usersWithTooManyPasswords)
            {
                // Mantener solo las últimas 10 contraseñas, ordenadas por fecha
                var passwordsToRemove = user.PasswordHistories
                    .OrderByDescending(ph => ph.PasswordChangedAt)
                    .Skip(maxPasswordHistoryPerUser)
                    .ToList();

                if (passwordsToRemove.Any())
                {
                    context.PasswordHistories.RemoveRange(passwordsToRemove);
                    totalRemoved += passwordsToRemove.Count;

                    _logger.LogDebug("Eliminando {Count} contraseñas antiguas para usuario {UserId}",
                        passwordsToRemove.Count, user.Id);
                }
            }

            // También eliminar contraseñas más antiguas del período de retención
            var oldPasswords = await context.PasswordHistories
                .Where(ph => ph.PasswordChangedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldPasswords.Any())
            {
                context.PasswordHistories.RemoveRange(oldPasswords);
                totalRemoved += oldPasswords.Count;

                _logger.LogInformation("Eliminando {Count} contraseñas más antiguas de {Date}",
                    oldPasswords.Count, cutoffDate);
            }

            if (totalRemoved > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Limpieza completada. {TotalRemoved} entradas de historial eliminadas",
                    totalRemoved);
            }
            else
            {
                _logger.LogInformation("Limpieza completada. No se encontraron entradas para eliminar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza del historial de contraseñas");
            throw;
        }
    }
}