using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Commands.CancelAccountDeletion;

public record CancelAccountDeletionCommand(
    CancelAccountDeletionRequest Request,
    string ClientIpAddress,
    string UserAgent
) : IRequest<CancelAccountDeletionResponse>;