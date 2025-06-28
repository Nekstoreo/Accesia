using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Commands.ChangeEmail;

public class ChangeEmailCommand : IRequest<ChangeEmailResponse>
{
    public ChangeEmailCommand(Guid userId, ChangeEmailRequest request, string clientIpAddress, string userAgent)
    {
        UserId = userId;
        NewEmail = request.NewEmail;
        CurrentPassword = request.CurrentPassword;
        Reason = request.Reason;
        ClientIpAddress = clientIpAddress;
        UserAgent = userAgent;
    }

    public Guid UserId { get; set; }
    public string NewEmail { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}