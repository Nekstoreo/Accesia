using Accesia.Domain.Entities;

namespace Accesia.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    DateTime GetTokenExpiration();
} 