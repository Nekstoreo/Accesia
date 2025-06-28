using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Features.Users.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeHandler : IRequestHandler<ConfirmEmailChangeCommand, ConfirmEmailChangeResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ConfirmEmailChangeHandler> _logger;

    public ConfirmEmailChangeHandler(IApplicationDbContext context, ILogger<ConfirmEmailChangeHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConfirmEmailChangeResponse> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirmando cambio de email a {NewEmail}", request.NewEmail);

        // 1. Buscar usuario con el token de verificación válido
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.VerificationToken, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Token de verificación {Token} no encontrado", request.VerificationToken);
            throw new InvalidVerificationTokenException("Token de verificación no válido", request.VerificationToken);
        }

        // 2. Verificar que el token sea válido y no haya expirado
        if (!user.IsEmailVerificationTokenValid(request.VerificationToken))
        {
            _logger.LogWarning("Token de verificación {Token} expirado para usuario {UserId}", 
                              request.VerificationToken, user.Id);
            throw new ExpiredVerificationTokenException("Token de verificación expirado", request.VerificationToken);
        }

        // 3. Validar que el nuevo email coincida con el solicitado
        var newEmail = new Email(request.NewEmail);

        // 4. Verificar nuevamente que el email no esté en uso por otro usuario
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == newEmail && u.Id != user.Id, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Email {NewEmail} ya está en uso por otro usuario", request.NewEmail);
            throw new EmailAlreadyExistsException(request.NewEmail);
        }

        // 5. Guardar el email anterior para auditoría
        var oldEmail = user.Email.Value;

        // 6. Confirmar el cambio de email
        user.ConfirmEmailChange(newEmail);

        // 7. Crear log de auditoría para la confirmación
        var auditLog = UserAuditLog.CreateEmailChange(
            user.Id, oldEmail, newEmail.Value,
            request.ClientIpAddress, request.UserAgent, "Email change confirmed");

        _context.UserAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cambio de email confirmado exitosamente para usuario {UserId}. Nuevo email: {NewEmail}", 
                              user.Id, request.NewEmail);

        // 8. Retornar perfil actualizado
        var updatedProfile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsPhoneVerified = user.IsPhoneVerified,
            PreferredLanguage = user.PreferredLanguage,
            TimeZone = user.TimeZone,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Status = user.Status.ToString()
        };

        return new ConfirmEmailChangeResponse
        {
            Success = true,
            Message = "El cambio de email se ha completado exitosamente",
            NewEmail = request.NewEmail,
            Profile = updatedProfile
        };
    }
} 