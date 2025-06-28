using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;

namespace Accesia.Infrastructure.Jobs;

public class SessionCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupJob> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1); // Ejecutar cada hora

    public SessionCleanupJob(IServiceProvider serviceProvider, ILogger<SessionCleanupJob> logger)
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
                _logger.LogError(ex, "Error durante la limpieza de sesiones");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando limpieza de sesiones expiradas");

        using var scope = _serviceProvider.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        try
        {
            await sessionService.CleanupExpiredSessionsAsync(cancellationToken);
            _logger.LogInformation("Limpieza de sesiones completada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de sesiones");
        }
    }
} 