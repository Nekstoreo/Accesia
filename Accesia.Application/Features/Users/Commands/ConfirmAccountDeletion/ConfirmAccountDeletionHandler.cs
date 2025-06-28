using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.Commands.ConfirmAccountDeletion;

public class ConfirmAccountDeletionHandler : IRequestHandler<ConfirmAccountDeletionCommand, ConfirmAccountDeletionResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<ConfirmAccountDeletionHandler> _logger;

    public ConfirmAccountDeletionHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ISessionService sessionService,
        ILogger<ConfirmAccountDeletionHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<ConfirmAccountDeletionResponse> Handle(ConfirmAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        // Buscar usuario por token de eliminación
        var user = await _context.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.AccountDeletionToken == request.Request.DeletionToken, cancellationToken);

        if (user == null)
            throw new BusinessRuleException("AccountDeletion", "TokenValidation", "Token de confirmación inválido o expirado");

        // Verificar que el token es válido
        if (!user.IsAccountDeletionTokenValid(request.Request.DeletionToken))
            throw new BusinessRuleException("AccountDeletion", "TokenValidation", "Token de confirmación inválido o expirado");

        try
        {
            // Confirmar eliminación (marca para eliminación)
            user.ConfirmAccountDeletion();

            // Revocar todas las sesiones activas
            await _sessionService.RevokeAllUserSessionsAsync(user.Id, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Calcular fecha de eliminación permanente (30 días por defecto)
            var permanentDeletionDate = user.GetPermanentDeletionDate(30);

            // Enviar email de confirmación de eliminación
            await _emailService.SendAccountMarkedForDeletionEmailAsync(
                user.Email.Value,
                $"{user.FirstName} {user.LastName}",
                permanentDeletionDate ?? DateTime.UtcNow.AddDays(30),
                cancellationToken);

            _logger.LogInformation("Usuario {UserId} confirmó eliminación desde IP {ClientIp}. Eliminación permanente: {PermanentDeletionDate}",
                user.Id, request.ClientIpAddress, permanentDeletionDate);

            return new ConfirmAccountDeletionResponse
            {
                Success = true,
                Message = "Su cuenta ha sido marcada para eliminación. Tiene 30 días para cancelar esta acción antes de que sea eliminada permanentemente",
                DeletedAt = DateTime.UtcNow,
                PermanentDeletionDate = permanentDeletionDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al confirmar eliminación para token {Token}", request.Request.DeletionToken);
            throw new BusinessRuleException("AccountDeletion", $"User:{user.Id}", "Error al procesar la confirmación de eliminación");
        }
    }
} 