using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Accesia.Application.Common.Exceptions;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no manejado en {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case AccountStateException accountEx:
                response = new ErrorResponse
                {
                    Title = "Error de Estado de Cuenta",
                    Status = (int)HttpStatusCode.Forbidden,
                    Detail = accountEx.Message,
                    Type = accountEx.GetType().Name,
                    Instance = context.Request.Path,
                    Extensions = new Dictionary<string, object>
                    {
                        ["userId"] = accountEx.UserId,
                        ["currentStatus"] = accountEx.CurrentStatus.ToString()
                    }
                };
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                break;

            case UserSettingsException settingsEx:
                response = new ErrorResponse
                {
                    Title = "Error de Configuración",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = settingsEx.Message,
                    Type = settingsEx.GetType().Name,
                    Instance = context.Request.Path,
                    Extensions = new Dictionary<string, object>
                    {
                        ["userId"] = settingsEx.UserId,
                        ["settingName"] = settingsEx.SettingName
                    }
                };
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case BusinessRuleException businessEx:
                response = new ErrorResponse
                {
                    Title = "Violación de Regla de Negocio",
                    Status = (int)HttpStatusCode.UnprocessableEntity,
                    Detail = businessEx.Message,
                    Type = businessEx.GetType().Name,
                    Instance = context.Request.Path,
                    Extensions = new Dictionary<string, object>
                    {
                        ["ruleName"] = businessEx.RuleName,
                        ["context"] = businessEx.Context
                    }
                };
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                break;

            case UserNotFoundException userNotFoundEx:
                response = new ErrorResponse
                {
                    Title = "Usuario No Encontrado",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = userNotFoundEx.Message,
                    Type = "UserNotFoundException",
                    Instance = context.Request.Path
                };
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case UnauthorizedAccessException:
                response = new ErrorResponse
                {
                    Title = "Acceso No Autorizado",
                    Status = (int)HttpStatusCode.Unauthorized,
                    Detail = "No tiene permisos para realizar esta acción",
                    Type = "UnauthorizedAccessException",
                    Instance = context.Request.Path
                };
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            default:
                response = new ErrorResponse
                {
                    Title = "Error Interno del Servidor",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = "Ha ocurrido un error inesperado",
                    Type = "InternalServerError",
                    Instance = context.Request.Path
                };
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, object>? Extensions { get; set; }
} 