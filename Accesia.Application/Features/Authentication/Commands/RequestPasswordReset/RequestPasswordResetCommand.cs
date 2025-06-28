using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.RequestPasswordReset;

public class RequestPasswordResetCommand : IRequest<RequestPasswordResetResponse>
{
    public string Email { get; set; } = string.Empty;
    public string? ClientIpAddress { get; set; }

    public RequestPasswordResetCommand(string email, string? clientIpAddress = null)
    {
        Email = email;
        ClientIpAddress = clientIpAddress;
    }
} 