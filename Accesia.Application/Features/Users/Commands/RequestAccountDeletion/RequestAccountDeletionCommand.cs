using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.RequestAccountDeletion;

public record RequestAccountDeletionCommand(
    Guid UserId,
    RequestAccountDeletionRequest Request,
    string ClientIpAddress,
    string UserAgent
) : IRequest<RequestAccountDeletionResponse>; 