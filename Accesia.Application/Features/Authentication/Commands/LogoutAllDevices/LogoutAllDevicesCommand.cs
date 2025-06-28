using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.LogoutAllDevices;

public record LogoutAllDevicesCommand : IRequest<LogoutAllDevicesResponse>
{
    public required string CurrentSessionToken { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }

    public static LogoutAllDevicesCommand FromRequest(LogoutAllDevicesRequest request, string ipAddress, string userAgent)
    {
        return new LogoutAllDevicesCommand
        {
            CurrentSessionToken = request.CurrentSessionToken,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
} 