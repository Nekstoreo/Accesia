using MediatR;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<RegisterUserResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Bogota";
    public string? ClientIpAddress { get; set; }

    public static RegisterUserCommand FromRequest(RegisterUserRequest request, string? ipAddress = null)
    {
        return new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            PreferredLanguage = request.PreferredLanguage,
            TimeZone = request.TimeZone,
            ClientIpAddress = ipAddress
        };
    }
}