using Accesia.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Accesia.Infrastructure.Services;

public class TokenService : ITokenService
{
    public string GenerateEmailVerificationToken()
    {
        return GenerateSecureToken(64); // Token más largo para verificación de email
    }
    public string GenerateSecureToken(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("La longitud debe ser mayor a 0", nameof(length));

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        // Convertir a string base64 URL-safe
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");  
    }
} 