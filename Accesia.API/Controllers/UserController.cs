using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Application.Features.Users.Queries.GetUserProfile;
using Accesia.Application.Features.Users.Commands.UpdateProfile;
using Accesia.Application.Features.Users.Commands.ChangeEmail;
using Accesia.Application.Features.Users.Commands.ConfirmEmailChange;
using Accesia.Application.Features.Users.Commands.ChangeAccountStatus;
using Accesia.Application.Features.Users.Commands.UpdateUserSettings;
using Accesia.Application.Features.Users.Commands.RequestAccountDeletion;
using Accesia.Application.Features.Users.Commands.CancelAccountDeletion;
using Accesia.Application.Features.Users.Commands.ConfirmAccountDeletion;
using Accesia.Application.Features.Users.Queries.GetAccountStatus;
using Accesia.Application.Features.Users.Queries.GetUserSettings;
using Accesia.Application.Features.Users.Queries.GetAccountDeletionStatus;
using Accesia.API.Attributes;

namespace Accesia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ValidateCsrf]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserController> _logger;

    public UserController(IMediator mediator, ILogger<UserController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el perfil del usuario actual
    /// </summary>
    /// <returns>Información del perfil del usuario</returns>
    [HttpGet("profile")]
    [EnableRateLimiting("UserProfilePolicy")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Solicitud de perfil para usuario {UserId}", userId);

        var query = new GetUserProfileQuery(userId);
        var profile = await _mediator.Send(query, cancellationToken);

        return Ok(profile);
    }

    /// <summary>
    /// Actualiza el perfil del usuario actual
    /// </summary>
    /// <param name="request">Datos del perfil a actualizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Perfil actualizado</returns>
    [HttpPut("profile")]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Actualizando perfil para usuario {UserId}", userId);

        var command = new UpdateProfileCommand(userId, request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Solicita cambio de email del usuario
    /// </summary>
    /// <param name="request">Solicitud de cambio de email</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta del cambio de email</returns>
    [HttpPost("change-email")]
    [EnableRateLimiting("EmailChangePolicy")]
    public async Task<ActionResult<ChangeEmailResponse>> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Solicitud de cambio de email para usuario {UserId}", userId);

        var command = new ChangeEmailCommand(userId, request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Confirma el cambio de email del usuario
    /// </summary>
    /// <param name="request">Confirmación del cambio de email</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta de confirmación</returns>
    [HttpPost("confirm-email-change")]
    [AllowAnonymous] // Permitir acceso anónimo para confirmar desde email
    [EnableRateLimiting("EmailConfirmationPolicy")]
    public async Task<ActionResult<ConfirmEmailChangeResponse>> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Confirmación de cambio de email para {NewEmail}", request.NewEmail);

        var command = new ConfirmEmailChangeCommand(request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene el estado actual de la cuenta del usuario
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Estado de la cuenta</returns>
    [HttpGet("account-status")]
    [EnableRateLimiting("UserProfilePolicy")]
    public async Task<ActionResult<GetAccountStatusResponse>> GetAccountStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Consulta de estado de cuenta para usuario {UserId}", userId);

        var query = new GetAccountStatusQuery(userId);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Cambia el estado de una cuenta de usuario (solo para administradores)
    /// </summary>
    /// <param name="request">Datos del cambio de estado</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado del cambio de estado</returns>
    [HttpPut("account-status")]
    [Authorize(Roles = "Admin")] // Solo administradores pueden cambiar estados
    [EnableRateLimiting("AdminPolicy")]
    public async Task<ActionResult<ChangeAccountStatusResponse>> ChangeAccountStatus(
        [FromBody] ChangeAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();
        var clientIpAddress = GetClientIpAddress();

        _logger.LogInformation("Admin {AdminId} cambiando estado de cuenta {UserId} a {NewStatus}", 
            adminUserId, request.UserId, request.NewStatus);

        var command = new ChangeAccountStatusCommand(request.UserId, request.NewStatus, request.Reason);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene las configuraciones del usuario actual
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Configuraciones del usuario</returns>
    [HttpGet("settings")]
    [EnableRateLimiting("UserProfilePolicy")]
    public async Task<ActionResult<GetUserSettingsResponse>> GetUserSettings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Consulta de configuraciones para usuario {UserId}", userId);

        var query = new GetUserSettingsQuery(userId);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Actualiza las configuraciones del usuario actual
    /// </summary>
    /// <param name="request">Nuevas configuraciones</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la actualización</returns>
    [HttpPut("settings")]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    public async Task<ActionResult<UpdateUserSettingsResponse>> UpdateUserSettings(
        [FromBody] UpdateUserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        request.UserId = userId; // Asegurar que solo puede actualizar sus propias configuraciones
        
        var clientIpAddress = GetClientIpAddress();

        _logger.LogInformation("Actualizando configuraciones para usuario {UserId}", userId);

        var command = new UpdateUserSettingsCommand(
            userId,
            request.NotificationSettings != null ? new NotificationSettings(
                request.NotificationSettings.EmailNotificationsEnabled,
                request.NotificationSettings.SmsNotificationsEnabled,
                request.NotificationSettings.PushNotificationsEnabled,
                request.NotificationSettings.InAppNotificationsEnabled,
                request.NotificationSettings.SecurityAlertsEnabled,
                request.NotificationSettings.LoginActivityNotificationsEnabled,
                request.NotificationSettings.PasswordChangeNotificationsEnabled,
                request.NotificationSettings.AccountUpdateNotificationsEnabled,
                request.NotificationSettings.SystemAnnouncementsEnabled,
                request.NotificationSettings.DeviceActivityNotificationsEnabled
            ) : null,
            request.PrivacySettings != null ? new PrivacySettings(
                request.PrivacySettings.ProfileVisibility,
                request.PrivacySettings.ShowLastLoginTime,
                request.PrivacySettings.ShowOnlineStatus,
                request.PrivacySettings.AllowDataCollection,
                request.PrivacySettings.AllowMarketingEmails
            ) : null,
            request.LocalizationSettings != null ? new LocalizationSettings(
                request.LocalizationSettings.PreferredLanguage,
                request.LocalizationSettings.TimeZone,
                request.LocalizationSettings.DateFormat,
                request.LocalizationSettings.TimeFormat
            ) : null,
            request.SecuritySettings != null ? new SecuritySettings(
                request.SecuritySettings.TwoFactorAuthEnabled,
                request.SecuritySettings.RequirePasswordChangeOn2FADisable,
                request.SecuritySettings.LogoutOnPasswordChange,
                request.SecuritySettings.SessionTimeoutMinutes
            ) : null
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Solicita la eliminación de la cuenta del usuario actual
    /// </summary>
    /// <param name="request">Datos de la solicitud de eliminación</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la solicitud</returns>
    [HttpPost("delete-account")]
    [EnableRateLimiting("AccountDeletionPolicy")]
    public async Task<ActionResult<RequestAccountDeletionResponse>> RequestAccountDeletion(
        [FromBody] RequestAccountDeletionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Solicitud de eliminación de cuenta para usuario {UserId} desde IP {ClientIp}",
            userId, clientIpAddress);

        var command = new RequestAccountDeletionCommand(userId, request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Cancela una solicitud de eliminación de cuenta
    /// </summary>
    /// <param name="request">Token de cancelación</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la cancelación</returns>
    [HttpPost("cancel-deletion")]
    [AllowAnonymous] // Permitir acceso anónimo para cancelar desde email
    [EnableRateLimiting("AccountDeletionPolicy")]
    public async Task<ActionResult<CancelAccountDeletionResponse>> CancelAccountDeletion(
        [FromBody] CancelAccountDeletionRequest request,
        CancellationToken cancellationToken)
    {
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Cancelación de eliminación desde IP {ClientIp} con token {Token}",
            clientIpAddress, request.CancellationToken);

        var command = new CancelAccountDeletionCommand(request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Confirma la eliminación permanente de la cuenta
    /// </summary>
    /// <param name="request">Token y confirmación final</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la confirmación</returns>
    [HttpPost("confirm-deletion")]
    [AllowAnonymous] // Permitir acceso anónimo para confirmar desde email
    [EnableRateLimiting("AccountDeletionPolicy")]
    public async Task<ActionResult<ConfirmAccountDeletionResponse>> ConfirmAccountDeletion(
        [FromBody] ConfirmAccountDeletionRequest request,
        CancellationToken cancellationToken)
    {
        var clientIpAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogInformation("Confirmación de eliminación desde IP {ClientIp} con token {Token}",
            clientIpAddress, request.DeletionToken);

        var command = new ConfirmAccountDeletionCommand(request, clientIpAddress, userAgent);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene el estado de eliminación de la cuenta del usuario actual
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Estado de eliminación de la cuenta</returns>
    [HttpGet("deletion-status")]
    [EnableRateLimiting("UserProfilePolicy")]
    public async Task<ActionResult<GetAccountDeletionStatusResponse>> GetAccountDeletionStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Consulta de estado de eliminación para usuario {UserId}", userId);

        var query = new GetAccountDeletionStatusQuery(userId);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuario no identificado");
        }
        return userId;
    }

    private string GetClientIpAddress()
    {
        // Intentar obtener la IP real del cliente considerando proxies y load balancers
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // Tomar la primera IP de la lista (la IP original del cliente)
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // Fallback a la conexión directa
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
} 