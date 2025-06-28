using MediatR;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.Enums;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.ChangeAccountStatus;

public class ChangeAccountStatusHandler : IRequestHandler<ChangeAccountStatusCommand, ChangeAccountStatusResponse>
{
    private readonly IApplicationDbContext _context;

    public ChangeAccountStatusHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChangeAccountStatusResponse> Handle(ChangeAccountStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        // Validar que la transición es permitida
        if (!user.CanTransitionTo(request.NewStatus))
        {
            throw new InvalidStateTransitionException(user.Id.ToString(), user.Status, request.NewStatus);
        }

        var previousStatus = user.Status;

        // Aplicar la transición según el nuevo estado
        switch (request.NewStatus)
        {
            case UserStatus.Active:
                user.ActivateAccount();
                break;
            case UserStatus.Inactive:
                user.DeactivateAccount();
                break;
            case UserStatus.Blocked:
                user.BlockAccount(request.Reason ?? "Bloqueado por administrador");
                break;
            case UserStatus.MarkedForDeletion:
                user.MarkForDeletion();
                break;
            default:
                throw new ArgumentException($"Estado no soportado para transición manual: {request.NewStatus}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangeAccountStatusResponse
        {
            UserId = user.Id,
            PreviousStatus = previousStatus,
            NewStatus = user.Status,
            StatusDescription = user.GetStatusDescription(),
            Message = $"Estado de cuenta cambiado exitosamente de '{GetStatusDescription(previousStatus)}' a '{user.GetStatusDescription()}'",
            Timestamp = DateTime.UtcNow
        };
    }

    private static string GetStatusDescription(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "Activa",
            UserStatus.Inactive => "Inactiva",
            UserStatus.Blocked => "Bloqueada",
            UserStatus.PendingConfirmation => "Pendiente de confirmación",
            UserStatus.EmailPendingVerification => "Verificación de email pendiente",
            UserStatus.MarkedForDeletion => "Marcada para eliminación",
            _ => "Desconocido"
        };
    }
} 