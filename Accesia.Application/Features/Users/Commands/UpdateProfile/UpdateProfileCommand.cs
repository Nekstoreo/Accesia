using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<UpdateProfileResponse>
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Bogota";
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public UpdateProfileCommand(Guid userId, UpdateProfileRequest request, string clientIpAddress, string userAgent)
    {
        UserId = userId;
        FirstName = request.FirstName;
        LastName = request.LastName;
        PhoneNumber = request.PhoneNumber;
        PreferredLanguage = request.PreferredLanguage;
        TimeZone = request.TimeZone;
        ClientIpAddress = clientIpAddress;
        UserAgent = userAgent;
    }
} 