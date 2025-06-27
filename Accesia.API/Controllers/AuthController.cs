using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Net;
using Accesia.Application.Features.Authentication.Commands.RegisterUser;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Exceptions;

namespace Accesia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema
    /// </summary>
    /// <param name="request">Datos del usuario a registrar</param>
    /// <returns>Información del usuario registrado</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener la IP del cliente
            var clientIp = GetClientIpAddress();
            
            // Crear el comando con la IP del cliente
            var command = RegisterUserCommand.FromRequest(request, clientIp);
            
            // Ejecutar el comando
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Usuario registrado exitosamente: {Email}", request.Email);
            
            return CreatedAtAction(
                nameof(Register), 
                new { email = response.Email }, 
                response);
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Intento de registro con email duplicado: {Email}", ex.Email);
            return Conflict(new ProblemDetails
            {
                Title = "Email ya registrado",
                Detail = ex.Message,
                Status = (int)HttpStatusCode.Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            });
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Rate limit excedido para IP {IP}", GetClientIpAddress());
            
            // Agregar header con información del retry
            Response.Headers.Append("Retry-After", ((int)ex.RetryAfter.TotalSeconds).ToString());
            
            return StatusCode((int)HttpStatusCode.TooManyRequests, new ProblemDetails
            {
                Title = "Demasiados intentos",
                Detail = ex.Message,
                Status = (int)HttpStatusCode.TooManyRequests,
                Type = "https://tools.ietf.org/html/rfc6585#section-4"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para registro");
            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = ex.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al registrar usuario");
            return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
            {
                Title = "Error interno del servidor",
                Detail = "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                Status = (int)HttpStatusCode.InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            });
        }
    }

    /// <summary>
    /// Obtiene la dirección IP del cliente considerando proxies y load balancers
    /// </summary>
    private string GetClientIpAddress()
    {
        // Verificar headers comunes de proxies
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Tomar la primera IP (IP real del cliente)
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedProto))
        {
            var clientIp = Request.Headers["CF-Connecting-IP"].FirstOrDefault(); // Cloudflare
            if (!string.IsNullOrEmpty(clientIp))
                return clientIp;
        }

        // Fallback a la IP de conexión remota
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
} 