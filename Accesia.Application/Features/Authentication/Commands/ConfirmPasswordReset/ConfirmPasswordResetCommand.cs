using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.ConfirmPasswordReset;

public class ConfirmPasswordResetCommand : IRequest<ConfirmPasswordResetResponse>
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;

    public ConfirmPasswordResetCommand(string token, string newPassword)
    {
        Token = token;
        NewPassword = newPassword;
    }
} 