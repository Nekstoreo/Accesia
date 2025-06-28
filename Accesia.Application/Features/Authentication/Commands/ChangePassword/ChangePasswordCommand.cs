using Accesia.Application.Features.Authentication.DTOs;
using MediatR;

namespace Accesia.Application.Features.Authentication.Commands.ChangePassword;

public class ChangePasswordCommand : IRequest<ChangePasswordResponse>
{
    public ChangePasswordCommand(Guid userId, string currentPassword, string newPassword, string clientIp = "",
        string userAgent = "")
    {
        UserId = userId;
        CurrentPassword = currentPassword;
        NewPassword = newPassword;
        ClientIp = clientIp;
        UserAgent = userAgent;
    }

    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}