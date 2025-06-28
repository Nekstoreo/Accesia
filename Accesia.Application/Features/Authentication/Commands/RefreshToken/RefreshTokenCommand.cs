using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public required string RefreshToken { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }

    public static RefreshTokenCommand FromRequest(RefreshTokenRequest request, string ipAddress, string userAgent)
    {
        return new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
} 