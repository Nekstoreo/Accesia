using Accesia.Application.Common.Interfaces;
using Accesia.Application.Settings;
using Microsoft.Extensions.Options;

namespace Accesia.Infrastructure.Services.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    private readonly PasswordHashSettings _settings;
    public BcryptPasswordHasher(IOptions<PasswordHashSettings> options) => _settings = options.Value;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, _settings.WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public bool NeedsRehash(string hashedPassword, int workFactor)
    {
        return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, workFactor);
    }
}