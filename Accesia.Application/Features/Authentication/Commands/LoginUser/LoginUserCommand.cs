using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.LoginUser;

public record LoginUserCommand : IRequest<LoginResponse>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
    public string? DeviceName { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }

    public static LoginUserCommand FromRequest(LoginRequest request, string ipAddress, string userAgent)
    {
        return new LoginUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe,
            DeviceName = request.DeviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
} 