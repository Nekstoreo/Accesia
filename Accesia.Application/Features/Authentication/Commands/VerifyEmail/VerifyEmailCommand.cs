using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.VerifyEmail;

public class VerifyEmailCommand : IRequest<VerifyEmailResponse>
{
    public string Token { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ClientIpAddress { get; set; }

    public static VerifyEmailCommand FromRequest(VerifyEmailRequest request, string? ipAddress = null)
    {
        return new VerifyEmailCommand
        {
            Token = request.Token,
            Email = request.Email,
            ClientIpAddress = ipAddress
        };
    }
}