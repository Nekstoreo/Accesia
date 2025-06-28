using Accesia.Application.Features.Authentication.DTOs;
using MediatR;

namespace Accesia.Application.Features.Authentication.Commands.RequestPasswordReset;

public class RequestPasswordResetCommand : IRequest<RequestPasswordResetResponse>
{
    public RequestPasswordResetCommand(string email, string? clientIpAddress = null)
    {
        Email = email;
        ClientIpAddress = clientIpAddress;
    }

    public string Email { get; set; } = string.Empty;
    public string? ClientIpAddress { get; set; }
}