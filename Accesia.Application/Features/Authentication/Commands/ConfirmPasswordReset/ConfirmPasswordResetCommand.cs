using Accesia.Application.Features.Authentication.DTOs;
using MediatR;

namespace Accesia.Application.Features.Authentication.Commands.ConfirmPasswordReset;

public class ConfirmPasswordResetCommand : IRequest<ConfirmPasswordResetResponse>
{
    public ConfirmPasswordResetCommand(string token, string newPassword)
    {
        Token = token;
        NewPassword = newPassword;
    }

    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}