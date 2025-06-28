using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Net;
using Accesia.Application.Features.Authentication.Commands.RegisterUser;
using Accesia.Application.Features.Authentication.Commands.LoginUser;
using Accesia.Application.Features.Authentication.Commands.RefreshToken;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Authentication.Commands.VerifyEmail;
using Accesia.Application.Features.Authentication.Commands.ResendVerificationEmail;
using Accesia.Application.Features.Authentication.Commands.Logout;
using Accesia.Application.Features.Authentication.Commands.LogoutAllDevices;

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
    /// Inicia sesión de un usuario con email y contraseña
    /// </summary>
    /// <param name="request">Datos de login del usuario</param>
    /// <returns>Información de la sesión y tokens de acceso</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Locked)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            var command = LoginUserCommand.FromRequest(request, clientIp, userAgent);
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Login exitoso para usuario {Email}", request.Email);
            
            return Ok(response);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Usuario no encontrado para login: {Email}", ex.Email);
            return Problem(
                detail: ex.Message,
                statusCode: 404,
                title: "Usuario no encontrado");
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning(ex, "Credenciales inválidas para {Email}", ex.Email);
            return Problem(
                detail: ex.Message,
                statusCode: 401,
                title: "Credenciales inválidas");
        }
        catch (AccountLockedException ex)
        {
            _logger.LogWarning(ex, "Cuenta bloqueada para {Email}", ex.Email);
            
            Response.Headers.Append("Retry-After", ((int)ex.RemainingLockTime.TotalSeconds).ToString());
            
            return Problem(
                detail: ex.Message,
                statusCode: 423,
                title: "Cuenta bloqueada");
        }
        catch (EmailNotVerifiedException ex)
        {
            _logger.LogWarning(ex, "Email no verificado para {Email}", ex.Email);
            return Problem(
                detail: ex.Message,
                statusCode: 403,
                title: "Email no verificado");
        }
        catch (RateLimitExceededException ex)
        {
            return HandleRateLimitExceeded(ex, "login");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para login");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al procesar login");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }

    /// <summary>
    /// Renueva el token de acceso usando un refresh token
    /// </summary>
    /// <param name="request">Datos del refresh token</param>
    /// <returns>Nuevos tokens de acceso</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Gone)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            var command = RefreshTokenCommand.FromRequest(request, clientIp, userAgent);
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Token renovado exitosamente desde IP {IpAddress}", clientIp);
            
            return Ok(response);
        }
        catch (InvalidVerificationTokenException ex)
        {
            _logger.LogWarning(ex, "Refresh token inválido desde IP {IpAddress}", GetClientIpAddress());
            return Problem(
                detail: ex.Message,
                statusCode: 401,
                title: "Token inválido");
        }
        catch (ExpiredVerificationTokenException ex)
        {
            _logger.LogWarning(ex, "Refresh token expirado desde IP {IpAddress}", GetClientIpAddress());
            return Problem(
                detail: ex.Message,
                statusCode: 410,
                title: "Token expirado");
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Usuario no encontrado para refresh token desde IP {IpAddress}", GetClientIpAddress());
            return Problem(
                detail: ex.Message,
                statusCode: 404,
                title: "Usuario no encontrado");
        }
        catch (RateLimitExceededException ex)
        {
            return HandleRateLimitExceeded(ex, "refresh token");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para refresh token");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al renovar token");
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

    /// <summary>
    /// Cierra la sesión del usuario invalidando el token JWT y la sesión almacenada
    /// </summary>
    /// <param name="request">Datos para el cierre de sesión</param>
    /// <returns>Respuesta del cierre de sesión</returns>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<LogoutResponse>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            var command = LogoutCommand.FromRequest(request, clientIp, userAgent);
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Logout procesado desde IP {IpAddress}", clientIp);
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para logout");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al procesar logout");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }

    /// <summary>
    /// Cierra sesión en todos los dispositivos del usuario invalidando todas las sesiones activas
    /// </summary>
    /// <param name="request">Datos para el cierre de sesión en todos los dispositivos</param>
    /// <returns>Respuesta del cierre de sesión en todos los dispositivos</returns>
    [HttpPost("logout-all-devices")]
    [ProducesResponseType(typeof(LogoutAllDevicesResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<LogoutAllDevicesResponse>> LogoutAllDevices(
        [FromBody] LogoutAllDevicesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            var command = LogoutAllDevicesCommand.FromRequest(request, clientIp, userAgent);
            var response = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("Logout de todos los dispositivos procesado desde IP {IpAddress}", clientIp);
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos de entrada inválidos para logout de todos los dispositivos");
            return BadRequest(new { 
                mensaje = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al procesar logout de todos los dispositivos");
            return Problem(
                detail: "Ha ocurrido un error inesperado. Por favor, intenta nuevamente más tarde.",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }
}