using Accesia.Application.Common.Exceptions;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accesia.Application.Features.Users.Queries.GetAccountDeletionStatus;

public class
    GetAccountDeletionStatusHandler : IRequestHandler<GetAccountDeletionStatusQuery, GetAccountDeletionStatusResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetAccountDeletionStatusHandler> _logger;

    public GetAccountDeletionStatusHandler(
        IApplicationDbContext context,
        ILogger<GetAccountDeletionStatusHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetAccountDeletionStatusResponse> Handle(GetAccountDeletionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new UserNotFoundException(request.UserId);

        const int gracePeriodDays = 30;
        var isMarkedForDeletion = user.Status == UserStatus.MarkedForDeletion;
        var isInGracePeriod = user.IsInGracePeriod();
        var permanentDeletionDate = user.GetPermanentDeletionDate();

        var daysRemaining = 0;
        if (isMarkedForDeletion && permanentDeletionDate.HasValue)
        {
            var remaining = permanentDeletionDate.Value - DateTime.UtcNow;
            daysRemaining = Math.Max(0, (int)remaining.TotalDays);
        }

        _logger.LogInformation("Consultando estado de eliminación para usuario {UserId}", request.UserId);

        return new GetAccountDeletionStatusResponse
        {
            IsMarkedForDeletion = isMarkedForDeletion,
            MarkedForDeletionAt = user.MarkedForDeletionAt,
            PermanentDeletionDate = permanentDeletionDate,
            DeletionReason = user.DeletionReason,
            IsInGracePeriod = isInGracePeriod,
            DaysRemainingInGracePeriod = daysRemaining,
            HasPendingDeletionRequest = !string.IsNullOrEmpty(user.AccountDeletionToken),
            DeletionTokenExpiresAt = user.AccountDeletionTokenExpiresAt
        };
    }
}