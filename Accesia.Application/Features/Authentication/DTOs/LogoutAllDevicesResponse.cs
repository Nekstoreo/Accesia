namespace Accesia.Application.Features.Authentication.DTOs;

public class LogoutAllDevicesResponse
{
    public required string Message { get; set; }
    public DateTime LogoutAt { get; set; }
    public int SessionsTerminated { get; set; }
    public bool Success { get; set; }
} 