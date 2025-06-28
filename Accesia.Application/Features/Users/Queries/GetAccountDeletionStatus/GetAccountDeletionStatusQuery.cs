using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Queries.GetAccountDeletionStatus;

public record GetAccountDeletionStatusQuery(Guid UserId) : IRequest<GetAccountDeletionStatusResponse>; 