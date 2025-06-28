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

// Configurar Rate Limiting
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
            },
            token);
    };

    // Política general para perfil de usuario (5 peticiones por minuto)
    options.AddTokenBucketLimiter("UserProfilePolicy", options =>
    {
        options.TokenLimit = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
        options.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        options.TokensPerPeriod = 5;
    });

    // Política para actualizaciones de perfil (2 peticiones por hora)
    options.AddFixedWindowLimiter("ProfileUpdatePolicy", options =>
    {
        options.PermitLimit = 2;
        options.Window = TimeSpan.FromHours(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
    });

    // Política para cambios de email (1 petición por día)
    options.AddFixedWindowLimiter("EmailChangePolicy", options =>
    {
        options.PermitLimit = 1;
        options.Window = TimeSpan.FromDays(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política para confirmación de cambio de email (3 intentos por hora)
    options.AddFixedWindowLimiter("EmailConfirmationPolicy", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromHours(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política para administradores (más permisiva)
    options.AddTokenBucketLimiter("AdminPolicy", options =>
    {
        options.TokenLimit = 50;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
        options.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        options.TokensPerPeriod = 50;
    });

    // Política para eliminación de cuenta (muy restrictiva - 1 solicitud por semana)
    options.AddFixedWindowLimiter("AccountDeletionPolicy", options =>
    {
        options.PermitLimit = 1;
        options.Window = TimeSpan.FromDays(7);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política para intentos de login (10 intentos por 5 minutos, luego se bloquea)
    options.AddSlidingWindowLimiter("LoginAttemptPolicy", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(5);
        options.SegmentsPerWindow = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0; // Sin cola, rechazar directamente
    });

    // Política para registros de usuario (3 registros por día por IP)
    options.AddFixedWindowLimiter("RegisterPolicy", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromDays(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Política para solicitudes de restablecimiento de contraseña (3 solicitudes por día)
    options.AddFixedWindowLimiter("PasswordResetPolicy", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromDays(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
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