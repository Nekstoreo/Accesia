using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Accesia.Infrastructure.Data;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Accesia.Infrastructure.Services;

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
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
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
        
        // Registrar servicios
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // TODO: Registrar repositorios cuando se implementen
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<ISessionRepository, SessionRepository>();
        // services.AddScoped<IRoleRepository, RoleRepository>();
        // services.AddScoped<IPermissionRepository, PermissionRepository>();

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