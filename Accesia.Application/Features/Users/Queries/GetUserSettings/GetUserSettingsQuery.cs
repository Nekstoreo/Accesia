using MediatR;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Queries.GetUserSettings;

public record GetUserSettingsQuery(Guid UserId) : IRequest<GetUserSettingsResponse>;