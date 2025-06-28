using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    public GetUserProfileQuery(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; set; }
}