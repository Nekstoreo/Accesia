using System.Text.RegularExpressions;
using Accesia.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Accesia.Infrastructure.Services;

public class PasswordSecurityService : IPasswordSecurityService
{
    // Diccionario básico de contraseñas comunes (en un escenario real esto vendría de un archivo o base de datos)
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456", "password", "123456789", "12345678", "12345", "1234567", "1234567890",
        "qwerty", "abc123", "111111", "password123", "admin", "letmein", "welcome",
        "monkey", "1234", "dragon", "123123", "baseball", "football", "iloveyou",
        "trustno1", "sunshine", "master", "123qwe", "shadow", "michael", "jennifer",
        "jordan", "superman", "harley", "robert", "matthew", "daniel", "anthony",
        "william", "david", "richard", "charles", "thomas", "christopher", "daniel",
        "contraseña", "password1", "administrador", "usuario", "clave", "secreto",
        "acceso", "sistema", "seguridad", "temporal", "prueba", "test", "demo"
    };

    // Patrones de contraseñas débiles
    private static readonly Regex[] WeakPatterns = new[]
    {
        new Regex(@"^(.)\1+$", RegexOptions.Compiled), // Todos los caracteres iguales
        new Regex(
            @"^(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase), // Secuencias
        new Regex(@"^(qwe|asd|zxc|qaz|wsx|edc)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase), // Patrones de teclado
        new Regex(@"^\d+$", RegexOptions.Compiled), // Solo números
        new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled) // Solo letras
    };

    private readonly ILogger<PasswordSecurityService> _logger;

    public PasswordSecurityService(ILogger<PasswordSecurityService> logger)
    {
        _logger = logger;
    }

    public bool IsPasswordSafe(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;

        // Verificar contra diccionario de contraseñas comunes
        if (CommonPasswords.Contains(password))
        {
            _logger.LogWarning("Contraseña encontrada en diccionario de contraseñas comunes");
            return false;
        }

        // Verificar variaciones comunes (con números al final)
        var basePassword = Regex.Replace(password, @"\d+$", "");
        if (basePassword.Length >= 3 && CommonPasswords.Contains(basePassword))
        {
            _logger.LogWarning("Contraseña es variación de contraseña común con números al final");
            return false;
        }

        // Verificar patrones débiles
        foreach (var pattern in WeakPatterns)
            if (pattern.IsMatch(password))
            {
                _logger.LogWarning("Contraseña coincide con patrón débil: {Pattern}", pattern.ToString());
                return false;
            }

        return true;
    }

    public async Task<bool> IsPasswordSafeAsync(string password, CancellationToken cancellationToken = default)
    {
        // Para esta implementación básica, la validación es sincrónica
        // En un escenario real podríamos consultar APIs externas o bases de datos grandes
        return await Task.FromResult(IsPasswordSafe(password));
    }

    public IEnumerable<string> GetPasswordSecuritySuggestions(string password)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            suggestions.Add("La contraseña no puede estar vacía");
            return suggestions;
        }

        if (password.Length < 8) suggestions.Add("Usa al menos 8 caracteres");

        if (!Regex.IsMatch(password, @"[A-Z]")) suggestions.Add("Incluye al menos una letra mayúscula");

        if (!Regex.IsMatch(password, @"[a-z]")) suggestions.Add("Incluye al menos una letra minúscula");

        if (!Regex.IsMatch(password, @"\d")) suggestions.Add("Incluye al menos un número");

        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}\|<>]"))
            suggestions.Add("Incluye al menos un carácter especial");

        if (CommonPasswords.Contains(password)) suggestions.Add("No uses contraseñas comunes o predecibles");

        if (WeakPatterns.Any(pattern => pattern.IsMatch(password)))
            suggestions.Add("Evita patrones predecibles como secuencias o repeticiones");

        if (password.ToLower().Contains("password") || password.ToLower().Contains("contraseña"))
            suggestions.Add("No incluyas palabras relacionadas con 'contraseña' o 'password'");

        return suggestions;
    }
}