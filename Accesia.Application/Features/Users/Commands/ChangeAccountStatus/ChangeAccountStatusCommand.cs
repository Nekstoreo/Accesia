using MediatR;
using Accesia.Domain.Enums;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.ChangeAccountStatus;

public record ChangeAccountStatusCommand(
    Guid UserId,
    UserStatus NewStatus,
    string? Reason = null
) : IRequest<ChangeAccountStatusResponse>; 