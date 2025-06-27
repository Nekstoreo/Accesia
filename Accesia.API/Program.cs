using Accesia.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/accesia-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Agregar servicios al contenedor
builder.Services.AddControllers();

// Configurar OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar servicios de infraestructura (Entity Framework, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

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

// Middleware de logging de requests
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Configurar health checks
app.MapHealthChecks("/health");

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
