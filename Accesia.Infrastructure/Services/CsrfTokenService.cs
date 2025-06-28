using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accesia.Infrastructure.Services;

public class CsrfTokenService : ICsrfTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<CsrfTokenService> _logger;
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(1); // Token válido por 1 hora

    public CsrfTokenService(ILogger<CsrfTokenService> logger, IOptions<JwtSettings> jwtSettings)
    {
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(Guid userId)
    {
        var tokenData = new CsrfTokenData
        {
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(_tokenLifetime),
            RandomValue = GenerateRandomString(32)
        };

        var json = JsonSerializer.Serialize(tokenData);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Encriptar el token usando HMAC
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var signature = hmac.ComputeHash(bytes);

        var token = Convert.ToBase64String(bytes) + "." + Convert.ToBase64String(signature);

        _logger.LogDebug("Token CSRF generado para usuario {UserId}", userId);
        return token;
    }

    public bool ValidateToken(string token, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token CSRF vacío para usuario {UserId}", userId);
            return false;
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Formato de token CSRF inválido para usuario {UserId}", userId);
                return false;
            }

            var dataBytes = Convert.FromBase64String(parts[0]);
            var providedSignature = Convert.FromBase64String(parts[1]);

            // Verificar firma
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var expectedSignature = hmac.ComputeHash(dataBytes);

            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                _logger.LogWarning("Firma de token CSRF inválida para usuario {UserId}", userId);
                return false;
            }

            // Deserializar y validar datos
            var json = Encoding.UTF8.GetString(dataBytes);
            var tokenData = JsonSerializer.Deserialize<CsrfTokenData>(json);

            if (tokenData == null)
            {
                _logger.LogWarning("Datos de token CSRF inválidos para usuario {UserId}", userId);
                return false;
            }

            if (tokenData.UserId != userId)
            {
                _logger.LogWarning(
                    "Usuario en token CSRF no coincide. Esperado: {ExpectedUserId}, Actual: {ActualUserId}",
                    userId, tokenData.UserId);
                return false;
            }

            if (DateTime.UtcNow > tokenData.ExpiresAt)
            {
                _logger.LogWarning("Token CSRF expirado para usuario {UserId}. Expiró el {ExpirationTime}",
                    userId, tokenData.ExpiresAt);
                return false;
            }

            _logger.LogDebug("Token CSRF válido para usuario {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar token CSRF para usuario {UserId}", userId);
            return false;
        }
    }

    public string? ExtractTokenFromHeaders(IDictionary<string, string> headers)
    {
        // Buscar en header X-CSRF-Token
        if (headers.TryGetValue("X-CSRF-Token", out var headerValue)) return headerValue;

        // Buscar en header X-XSRF-TOKEN (Angular/otros frameworks)
        if (headers.TryGetValue("X-XSRF-TOKEN", out var xsrfValue)) return xsrfValue;

        return null;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private class CsrfTokenData
    {
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string RandomValue { get; set; } = string.Empty;
    }
}