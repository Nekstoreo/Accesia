using Accesia.Application.Features.Users.DTOs;
using MediatR;

namespace Accesia.Application.Features.Users.Queries.GetUserSettings;

public record GetUserSettingsQuery(Guid UserId) : IRequest<GetUserSettingsResponse>;