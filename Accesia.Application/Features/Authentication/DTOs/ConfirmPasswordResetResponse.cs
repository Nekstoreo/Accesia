namespace Accesia.Application.Features.Authentication.DTOs;

public class ConfirmPasswordResetResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}