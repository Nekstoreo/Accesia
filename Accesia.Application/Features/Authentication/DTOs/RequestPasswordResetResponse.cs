namespace Accesia.Application.Features.Authentication.DTOs;

public class RequestPasswordResetResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
} 