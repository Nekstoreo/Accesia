using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;
using MediatR;

namespace Accesia.Application.Features.Users.Commands.ChangeAccountStatus;

public record ChangeAccountStatusCommand(
    Guid UserId,
    UserStatus NewStatus,
    string? Reason = null
) : IRequest<ChangeAccountStatusResponse>;