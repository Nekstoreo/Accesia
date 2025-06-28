using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Users.Commands.CancelAccountDeletion;

public class CancelAccountDeletionHandler : IRequestHandler<CancelAccountDeletionCommand, CancelAccountDeletionResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CancelAccountDeletionHandler> _logger;

    public CancelAccountDeletionHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<CancelAccountDeletionHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CancelAccountDeletionResponse> Handle(CancelAccountDeletionCommand request,
        CancellationToken cancellationToken)
    {
        // Buscar usuario por token de cancelación
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.AccountDeletionToken == request.Request.CancellationToken, cancellationToken);

        if (user == null)
            throw new BusinessRuleException("AccountDeletion", "TokenValidation",
                "Token de cancelación inválido o expirado");

        // Verificar que el token es válido
        if (!user.IsAccountDeletionTokenValid(request.Request.CancellationToken))
            throw new BusinessRuleException("AccountDeletion", "TokenValidation",
                "Token de cancelación inválido o expirado");

        try
        {
            var wasMarkedForDeletion = user.Status == UserStatus.MarkedForDeletion;

            // Cancelar eliminación
            user.CancelAccountDeletion();

            await _context.SaveChangesAsync(cancellationToken);

            // Enviar email de confirmación de cancelación
            await _emailService.SendAccountDeletionCancelledEmailAsync(
                user.Email.Value,
                $"{user.FirstName} {user.LastName}",
                cancellationToken);

            _logger.LogInformation("Eliminación cancelada para usuario {UserId} desde IP {ClientIp}",
                user.Id, request.ClientIpAddress);

            return new CancelAccountDeletionResponse
            {
                Success = true,
                Message = wasMarkedForDeletion
                    ? "Su cuenta ha sido restaurada exitosamente. Necesitará reactivarla para usar el sistema"
                    : "La solicitud de eliminación ha sido cancelada exitosamente",
                RestoredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar eliminación para token {Token}", request.Request.CancellationToken);
            throw new BusinessRuleException("AccountDeletion", $"User:{user.Id}",
                "Error al procesar la cancelación de eliminación");
        }
    }
}