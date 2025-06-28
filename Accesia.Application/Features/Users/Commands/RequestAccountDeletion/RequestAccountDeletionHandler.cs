using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.Commands.RequestAccountDeletion;

public class RequestAccountDeletionHandler : IRequestHandler<RequestAccountDeletionCommand, RequestAccountDeletionResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestAccountDeletionHandler> _logger;

    public RequestAccountDeletionHandler(
        IApplicationDbContext context,
        IPasswordHashService passwordHashService,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<RequestAccountDeletionHandler> logger)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RequestAccountDeletionResponse> Handle(RequestAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        // Verificar que el usuario puede realizar esta acción
        if (!user.CanPerformAction())
            throw new AccountStateException(user.Id.ToString(), user.Status, "No puede solicitar eliminación en el estado actual de la cuenta");

        // Verificar contraseña actual
        if (!_passwordHashService.VerifyPassword(request.Request.CurrentPassword, user.PasswordHash))
            throw new CurrentPasswordIncorrectException();

        // Verificar si ya hay una solicitud pendiente
        if (!string.IsNullOrEmpty(user.AccountDeletionToken))
            throw new BusinessRuleException("AccountDeletion", $"User:{user.Id}", "Ya existe una solicitud de eliminación pendiente");

        // Generar token de eliminación
        var deletionToken = _tokenService.GenerateSecureToken();
        var tokenExpiration = DateTime.UtcNow.AddHours(24); // 24 horas para confirmar

        try
        {
            // Configurar solicitud de eliminación
            user.RequestAccountDeletion(deletionToken, tokenExpiration, request.Request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            // Enviar email de confirmación
            await _emailService.SendAccountDeletionConfirmationEmailAsync(
                user.Email.Value,
                $"{user.FirstName} {user.LastName}",
                deletionToken,
                tokenExpiration,
                cancellationToken);

            _logger.LogInformation("Solicitud de eliminación creada para usuario {UserId} desde IP {ClientIp}",
                request.UserId, request.ClientIpAddress);

            return new RequestAccountDeletionResponse
            {
                Success = true,
                Message = "Se ha enviado un email de confirmación para proceder con la eliminación de su cuenta",
                TokenExpiresAt = tokenExpiration,
                EmailSent = user.Email.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar solicitud de eliminación para usuario {UserId}", request.UserId);
            throw new BusinessRuleException("AccountDeletion", $"User:{request.UserId}", "Error al procesar la solicitud de eliminación");
        }
    }
} 