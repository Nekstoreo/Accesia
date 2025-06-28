using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    public Guid UserId { get; set; }

    public GetUserProfileQuery(Guid userId)
    {
        UserId = userId;
    }
} 