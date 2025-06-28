using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserProfileHandler> _logger;

    public GetUserProfileHandler(IApplicationDbContext context, ILogger<GetUserProfileHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Obteniendo perfil para usuario {UserId}", request.UserId);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", request.UserId);
            throw new UserNotFoundException(request.UserId);
        }

        return new UserProfileDto
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
    }
} 