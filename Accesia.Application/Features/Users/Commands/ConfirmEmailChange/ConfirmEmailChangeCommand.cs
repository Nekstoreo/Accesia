using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeCommand : IRequest<ConfirmEmailChangeResponse>
{
    public string NewEmail { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public ConfirmEmailChangeCommand(ConfirmEmailChangeRequest request, string clientIpAddress, string userAgent)
    {
        NewEmail = request.NewEmail;
        VerificationToken = request.VerificationToken;
        ClientIpAddress = clientIpAddress;
        UserAgent = userAgent;
    }
} 