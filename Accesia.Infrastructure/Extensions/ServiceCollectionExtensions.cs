using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Accesia.Infrastructure.Data;
using Accesia.Infrastructure.Jobs;
using Accesia.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar DbContext con PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        3,
                        TimeSpan.FromSeconds(10),
                        null);
                });

            // Configuraciones de desarrollo
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
        });

        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        // Configurar health checks
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "postgresql",
                tags: new[] { "database", "postgresql" });

        // Configurar JWT Settings
        services.Configure<JwtSettings>(options =>
            configuration.GetSection(JwtSettings.SectionName).Bind(options));

        // Configurar Security Settings
        services.Configure<SecuritySettings>(options =>
            configuration.GetSection(SecuritySettings.SectionName).Bind(options));

        // Registrar servicios principales
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IPasswordSecurityService, PasswordSecurityService>();
        services.AddScoped<ICsrfTokenService, CsrfTokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IDeviceInfoService, DeviceInfoService>();

        // Servicios de seguridad y auditoría
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<ISecurityAlertService, SecurityAlertService>();
        services.AddScoped<ISecuritySearchService, SecuritySearchService>();
        services.AddScoped<ILogIntegrityService, LogIntegrityService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IAdvancedRateLimitService, AdvancedRateLimitService>();

        // Registrar Memory Cache para rate limiting
        services.AddMemoryCache();

        // Registrar background jobs
        services.AddHostedService<TokenCleanupJob>();
        services.AddHostedService<SessionCleanupJob>();
        services.AddHostedService<AccountDeletionJob>();
        services.AddHostedService<AuditLogCleanupJob>();

        return services;
    }

    public static async Task MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();
            logger?.LogError(ex, "Error durante la migración de la base de datos");
            throw;
        }
    }
}