using Accesia.Application.Common.Interfaces;
using BCrypt.Net;

namespace Accesia.Infrastructure.Services;

public class PasswordHashService : IPasswordHashService
{
    private const int WorkFactor = 12; // Factor de trabajo de BCrypt (más alto = más seguro pero más lento)

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

        // BCrypt.Net genera automáticamente un salt único para cada hash
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Si hay algún error en la verificación, retornamos false por seguridad
            return false;
        }
    }
} 