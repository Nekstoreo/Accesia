using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateProfileHandler> _logger;

    public UpdateProfileHandler(IApplicationDbContext context, ILogger<UpdateProfileHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UpdateProfileResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Actualizando perfil para usuario {UserId}", request.UserId);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", request.UserId);
            throw new UserNotFoundException(request.UserId);
        }

        // Crear logs de auditoría para cambios
        var auditLogs = new List<UserAuditLog>();

        // Verificar y auditar cambios
        if (user.FirstName != request.FirstName)
            auditLogs.Add(UserAuditLog.CreateProfileUpdate(
                user.Id, "FirstName", user.FirstName, request.FirstName,
                request.ClientIpAddress, request.UserAgent));

        if (user.LastName != request.LastName)
            auditLogs.Add(UserAuditLog.CreateProfileUpdate(
                user.Id, "LastName", user.LastName, request.LastName,
                request.ClientIpAddress, request.UserAgent));

        if (user.PhoneNumber != request.PhoneNumber)
            auditLogs.Add(UserAuditLog.CreateProfileUpdate(
                user.Id, "PhoneNumber", user.PhoneNumber, request.PhoneNumber,
                request.ClientIpAddress, request.UserAgent));

        if (user.PreferredLanguage != request.PreferredLanguage)
            auditLogs.Add(UserAuditLog.CreateProfileUpdate(
                user.Id, "PreferredLanguage", user.PreferredLanguage, request.PreferredLanguage,
                request.ClientIpAddress, request.UserAgent));

        if (user.TimeZone != request.TimeZone)
            auditLogs.Add(UserAuditLog.CreateProfileUpdate(
                user.Id, "TimeZone", user.TimeZone, request.TimeZone,
                request.ClientIpAddress, request.UserAgent));

        // Actualizar el perfil del usuario
        user.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.PreferredLanguage,
            request.TimeZone
        );

        // Agregar logs de auditoría
        foreach (var auditLog in auditLogs) _context.UserAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Perfil actualizado exitosamente para usuario {UserId}", request.UserId);

        // Retornar perfil actualizado
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

        return new UpdateProfileResponse
        {
            Success = true,
            Message = "Perfil actualizado exitosamente",
            Profile = updatedProfile
        };
    }
}