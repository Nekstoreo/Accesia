using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Users.Commands.ChangeEmail;

public class ChangeEmailHandler : IRequestHandler<ChangeEmailCommand, ChangeEmailResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ChangeEmailHandler> _logger;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;

    public ChangeEmailHandler(
        IApplicationDbContext context,
        IPasswordHashService passwordHashService,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<ChangeEmailHandler> logger)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ChangeEmailResponse> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando cambio de email para usuario {UserId} a {NewEmail}",
            request.UserId, request.NewEmail);

        // 1. Obtener el usuario
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", request.UserId);
            throw new UserNotFoundException(request.UserId);
        }

        // 2. Verificar contraseña actual
        if (!_passwordHashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Contraseña incorrecta para usuario {UserId} en cambio de email", request.UserId);
            throw new CurrentPasswordIncorrectException();
        }

        // 3. Validar que el nuevo email sea diferente
        var newEmail = new Email(request.NewEmail);
        if (newEmail.Value == user.Email.Value)
            throw new InvalidOperationException("El nuevo email debe ser diferente al actual.");

        // 4. Verificar que el nuevo email no esté en uso
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == newEmail && u.Id != request.UserId, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Email {NewEmail} ya está en uso", request.NewEmail);
            throw new EmailAlreadyExistsException(request.NewEmail);
        }

        // 5. Generar token de verificación
        var verificationToken = _tokenService.GenerateEmailVerificationToken();
        var tokenExpiration = DateTime.UtcNow.AddHours(24);

        // 6. Iniciar el proceso de cambio de email
        user.InitiateEmailChange(newEmail, verificationToken, tokenExpiration);

        // 7. Crear log de auditoría
        var auditLog = UserAuditLog.CreateEmailChange(
            user.Id, user.Email.Value, newEmail.Value,
            request.ClientIpAddress, request.UserAgent, request.Reason);

        _context.UserAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // 8. Enviar email de verificación
        await _emailService.SendEmailChangeVerificationAsync(
            request.NewEmail, user.FirstName, verificationToken);

        _logger.LogInformation("Email de verificación enviado a {NewEmail} para usuario {UserId}",
            request.NewEmail, request.UserId);

        return new ChangeEmailResponse
        {
            Success = true,
            Message =
                "Se ha enviado un email de verificación a la nueva dirección. Verifica tu email para completar el cambio.",
            NewEmail = request.NewEmail,
            RequiresVerification = true
        };
    }
}