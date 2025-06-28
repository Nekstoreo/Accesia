using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.Logout;

public record LogoutCommand : IRequest<LogoutResponse>
{
    public required string SessionToken { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }

    public static LogoutCommand FromRequest(LogoutRequest request, string ipAddress, string userAgent)
    {
        return new LogoutCommand
        {
            SessionToken = request.SessionToken,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
} 