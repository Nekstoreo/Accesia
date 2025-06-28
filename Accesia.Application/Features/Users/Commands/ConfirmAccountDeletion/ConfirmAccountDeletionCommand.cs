using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Commands.ConfirmAccountDeletion;

public record ConfirmAccountDeletionCommand(
    ConfirmAccountDeletionRequest Request,
    string ClientIpAddress,
    string UserAgent
) : IRequest<ConfirmAccountDeletionResponse>;