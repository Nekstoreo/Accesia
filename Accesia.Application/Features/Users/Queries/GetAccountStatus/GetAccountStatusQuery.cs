using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Queries.GetAccountStatus;

public record GetAccountStatusQuery(Guid UserId) : IRequest<GetAccountStatusResponse>; 