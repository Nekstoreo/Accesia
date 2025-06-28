namespace Accesia.Application.Features.Authentication.DTOs;

public class LogoutResponse
{
    public required string Message { get; set; }
    public DateTime LogoutAt { get; set; }
    public bool Success { get; set; }
} 