using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.ConfirmAccountDeletion;

public record ConfirmAccountDeletionCommand(
    ConfirmAccountDeletionRequest Request,
    string ClientIpAddress,
    string UserAgent
) : IRequest<ConfirmAccountDeletionResponse>; 