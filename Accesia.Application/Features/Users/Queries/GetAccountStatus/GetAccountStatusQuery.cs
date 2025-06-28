using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Queries.GetAccountStatus;

public record GetAccountStatusQuery(Guid UserId) : IRequest<GetAccountStatusResponse>;