using System.Text;
using System.Threading.RateLimiting;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Settings;
using Accesia.Application.Extensions;
using Accesia.Infrastructure.Data;
using Accesia.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Agregar servicios al contenedor
builder.Services.AddControllers();

// Configurar OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar servicios de Application (MediatR, FluentValidation, etc.)
builder.Services.AddApplication();

// Agregar servicios de infraestructura (Entity Framework, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Configurar Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Configurar JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings!.SecretKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Solo para desarrollo
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// enlazar SecuritySettings desde configuración
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection(SecuritySettings.SectionName));

// configurar rate limiting basado en SecuritySettings
var securitySettings = builder.Configuration.GetSection(SecuritySettings.SectionName).Get<SecuritySettings>()!;
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var logMessage = $"Rate limit alcanzado para endpoint {context.HttpContext.Request.Path}. " +
                         $"Usuario: {context.HttpContext.User.Identity?.Name ?? "Anónimo"}, " +
                         $"IP: {context.HttpContext.Connection.RemoteIpAddress}";
        context.HttpContext.RequestServices.GetService<ILogger<Program>>()?.LogWarning(logMessage);
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                title = "Demasiadas solicitudes",
                detail = "Has excedido el límite de solicitudes permitidas. Por favor, intenta más tarde."
            }, token);
    };

    foreach (var kvp in securitySettings.RateLimit.Policies)
    {
        var policyName = $"{kvp.Key}Policy";
        var p = kvp.Value;
        switch (p.Type)
        {
            case "FixedWindow":
                options.AddFixedWindowLimiter(policyName, opts =>
                {
                    opts.PermitLimit = p.MaxAttempts;
                    opts.Window = TimeSpan.FromMinutes(p.WindowMinutes);
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.QueueLimit = p.AdditionalSettings.ContainsKey("QueueLimit") ? Convert.ToInt32(p.AdditionalSettings["QueueLimit"]) : 0;
                });
                break;
            case "SlidingWindow":
                options.AddSlidingWindowLimiter(policyName, opts =>
                {
                    opts.PermitLimit = p.MaxAttempts;
                    opts.Window = TimeSpan.FromMinutes(p.WindowMinutes);
                    opts.SegmentsPerWindow = p.Segments;
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.QueueLimit = p.AdditionalSettings.ContainsKey("QueueLimit") ? Convert.ToInt32(p.AdditionalSettings["QueueLimit"]) : 0;
                });
                break;
            case "TokenBucket":
                options.AddTokenBucketLimiter(policyName, opts =>
                {
                    opts.TokenLimit = p.TokensPerPeriod;
                    opts.ReplenishmentPeriod = TimeSpan.FromMinutes(p.ReplenishmentPeriodMinutes);
                    opts.TokensPerPeriod = p.TokensPerPeriod;
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.QueueLimit = p.AdditionalSettings.ContainsKey("QueueLimit") ? Convert.ToInt32(p.AdditionalSettings["QueueLimit"]) : 0;
                });
                break;
        }
    }
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDevelopment", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Leer LOG_INTEGRITY_SECRET desde configuración si no está en el entorno
var logIntegritySecret = Environment.GetEnvironmentVariable("LOG_INTEGRITY_SECRET");
if (string.IsNullOrEmpty(logIntegritySecret))
{
    // Buscar en la configuración (appsettings.Development.json, etc.)
    var configSecret = builder.Configuration["Security:LogIntegrity:SecretKey"];
    if (!string.IsNullOrEmpty(configSecret))
    {
        Environment.SetEnvironmentVariable("LOG_INTEGRITY_SECRET", configSecret);
        logIntegritySecret = configSecret;
    }
}

var app = builder.Build();

// Ejecutar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowDevelopment");

    // Migrar base de datos automáticamente
    try
    {
        await app.Services.MigrateDatabase();
        Log.Information("Base de datos migrada exitosamente");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Error al migrar la base de datos");
        throw;
    }
}

app.UseHttpsRedirection();

// Middleware de manejo de excepciones (debe ir antes que otros middlewares)
app.UseMiddleware<ExceptionMiddleware>();

// Middleware de logging de requests
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configurar health checks
app.MapHealthChecks("/health");

// Validar configuraciones críticas de seguridad al inicio
try
{
    // Validar que la clave de integridad esté configurada
    // Esta variable debe configurarse en el entorno o en un archivo .env
    // Ejemplo: LOG_INTEGRITY_SECRET=your_integrity_secret_key_at_least_32_chars_long
    var integritySecret = Environment.GetEnvironmentVariable("LOG_INTEGRITY_SECRET");
    if (string.IsNullOrEmpty(integritySecret))
    {
        throw new InvalidOperationException(
            "LOG_INTEGRITY_SECRET environment variable is required for security audit log integrity. " +
            "Please configure this environment variable with a secure random key of at least 32 characters.");
    }

    if (integritySecret.Length < 32)
    {
        throw new InvalidOperationException(
            "LOG_INTEGRITY_SECRET must be at least 32 characters long for adequate security.");
    }

    app.Logger.LogInformation("Security configuration validated successfully");
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Critical security configuration error. Application cannot start safely.");
    throw;
}

try
{
    Log.Information("Iniciando Accesia API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal al iniciar la aplicación");
}
finally
{
    Log.CloseAndFlush();
}