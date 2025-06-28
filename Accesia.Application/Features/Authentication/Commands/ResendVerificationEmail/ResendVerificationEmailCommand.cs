using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.ResendVerificationEmail;

public class ResendVerificationEmailCommand : IRequest<ResendVerificationResponse>
{
    public string Email { get; set; } = string.Empty;
    public string? ClientIpAddress { get; set; }

    public static ResendVerificationEmailCommand FromRequest(ResendVerificationRequest request, string? ipAddress = null)
    {
        return new ResendVerificationEmailCommand
        {
            Email = request.Email,
            ClientIpAddress = ipAddress
        };
    }
}
