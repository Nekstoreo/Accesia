using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.CancelAccountDeletion;

public record CancelAccountDeletionCommand(
    CancelAccountDeletionRequest Request,
    string ClientIpAddress,
    string UserAgent
) : IRequest<CancelAccountDeletionResponse>; 