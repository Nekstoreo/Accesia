using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Net;
using Accesia.Application.Features.Authentication.Commands.RegisterUser;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Authentication.Commands.VerifyEmail;
using Accesia.Application.Features.Authentication.Commands.ResendVerificationEmail;

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
            return Conflict(new { 
                mensaje = ex.Message,
                email = ex.Email,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (RateLimitExceededException ex)
        {
            return HandleRateLimitExceeded(ex, "registro");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para registro");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al registrar usuario");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }

    /// <summary>
    /// Reenvía el email de verificación al usuario
    /// </summary>
    /// <param name="request">Datos para el reenvío de verificación</param>
    /// <returns>Respuesta del reenvío de verificación</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ResendVerificationResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<ActionResult<ResendVerificationResponse>> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener la IP del cliente
            var clientIp = GetClientIpAddress();
            
            // Crear el comando con la IP del cliente
            var command = ResendVerificationEmailCommand.FromRequest(request, clientIp);
            
            // Ejecutar el comando
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Email de verificación reenviado exitosamente a {Email}", request.Email);
            
            return Ok(response);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Usuario no encontrado para reenvío: {Email}", ex.Email);
            return Problem(
                detail: ex.Message,
                statusCode: 404,
                title: "Usuario no encontrado");
        }
        catch (EmailAlreadyVerifiedException ex)
        {
            _logger.LogWarning(ex, "Intento de reenvío para email ya verificado: {Email}", ex.Email);
            return Conflict(new { 
                mensaje = ex.Message,
                email = ex.Email,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (RateLimitExceededException ex)
        {
            return HandleRateLimitExceeded(ex, "reenvío de verificación");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para reenvío");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al reenviar verificación");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }

    /// <summary>
    /// Maneja las excepciones de rate limiting de manera centralizada
    /// </summary>
    /// <param name="ex">Excepción de rate limiting</param>
    /// <param name="action">Acción que fue limitada</param>
    /// <returns>Respuesta HTTP con código 429</returns>
    private ActionResult HandleRateLimitExceeded(RateLimitExceededException ex, string action)
    {
        _logger.LogWarning(ex, "Rate limit excedido para {Action} desde IP {IP}", action, GetClientIpAddress());
        
        // Agregar header con información del retry
        Response.Headers.Append("Retry-After", ((int)ex.RetryAfter.TotalSeconds).ToString());
        
        return Problem(
            detail: ex.Message,
            statusCode: 429,
            title: "Demasiados intentos");
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

    /// <summary>
    /// Verifica el correo electrónico del usuario
    /// </summary>
    /// <param name="request">Datos de verificación del correo electrónico</param>
    /// <returns>Respuesta de verificación del correo electrónico</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Gone)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener la IP del cliente
            var clientIp = GetClientIpAddress();
            
            // Crear el comando con la IP del cliente
            var command = VerifyEmailCommand.FromRequest(request, clientIp);
            
            // Ejecutar el comando
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Correo electrónico verificado exitosamente");
            
            return Ok(response);
        }
        catch (InvalidVerificationTokenException ex)
        {
            _logger.LogWarning(ex, "Token de verificación inválido: {Token}", ex.Token);
            return Problem(
                detail: ex.Message,
                statusCode: 404,
                title: "Token de verificación inválido");
        }
        catch (ExpiredVerificationTokenException ex)
        {
            _logger.LogWarning(ex, "Token de verificación expirado: {Token}", ex.Token);
            return Problem(
                detail: ex.Message,
                statusCode: 410,
                title: "Token de verificación expirado");
        }
        catch (EmailAlreadyVerifiedException ex)
        {
            _logger.LogWarning(ex, "Correo electrónico ya verificado: {Email}", ex.Email);
            return Conflict(new { 
                mensaje = ex.Message,
                email = ex.Email,
                token = ex.Token,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (RateLimitExceededException ex)
        {
            return HandleRateLimitExceeded(ex, "verificación de email");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para verificación");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al verificar correo electrónico");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }
}