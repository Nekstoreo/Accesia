namespace Accesia.Application.Common.Interfaces;

public interface IPasswordSecurityService
{
    /// <summary>
    /// Valida que la contraseña no esté en diccionarios comunes
    /// </summary>
    /// <param name="password">Contraseña a validar</param>
    /// <returns>True si la contraseña es segura, False si está en diccionarios comunes</returns>
    bool IsPasswordSafe(string password);

    /// <summary>
    /// Valida que la contraseña no esté en diccionarios comunes de forma asíncrona
    /// </summary>
    /// <param name="password">Contraseña a validar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si la contraseña es segura, False si está en diccionarios comunes</returns>
    Task<bool> IsPasswordSafeAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene sugerencias para mejorar la seguridad de la contraseña
    /// </summary>
    /// <param name="password">Contraseña a analizar</param>
    /// <returns>Lista de sugerencias de mejora</returns>
    IEnumerable<string> GetPasswordSecuritySuggestions(string password);
} 