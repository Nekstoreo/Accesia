using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.ChangePassword;

public class ChangePasswordCommand : IRequest<ChangePasswordResponse>
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;

    public ChangePasswordCommand(Guid userId, string currentPassword, string newPassword)
    {
        UserId = userId;
        CurrentPassword = currentPassword;
        NewPassword = newPassword;
    }
} 