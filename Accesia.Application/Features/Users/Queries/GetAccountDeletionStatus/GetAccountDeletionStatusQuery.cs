using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Queries.GetAccountDeletionStatus;

public record GetAccountDeletionStatusQuery(Guid UserId) : IRequest<GetAccountDeletionStatusResponse>;