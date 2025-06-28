using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accesia.Application.Features.Users.Queries.GetAccountStatus;

public class GetAccountStatusHandler : IRequestHandler<GetAccountStatusQuery, GetAccountStatusResponse>
{
    private readonly IApplicationDbContext _context;

    public GetAccountStatusHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetAccountStatusResponse> Handle(GetAccountStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        return new GetAccountStatusResponse
        {
            UserId = user.Id,
            CurrentStatus = user.Status,
            StatusDescription = user.GetStatusDescription(),
            IsAccountLocked = user.IsAccountLocked(),
            LockedUntil = user.LockedUntil,
            FailedLoginAttempts = user.FailedLoginAttempts,
            CanLogin = user.CanLogin(),
            CanPerformAction = user.CanPerformAction(),
            RequiresReactivation = user.RequiresReactivation(),
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            LastLoginAt = user.LastLoginAt,
            AllowedTransitions = user.GetAllowedTransitions().ToList()
        };
    }
}