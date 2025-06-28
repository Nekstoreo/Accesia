using System.Text.RegularExpressions;
using FluentValidation;

namespace Accesia.Application.Common.Validators;

public static class SecurityValidators
{
    // Patrones para detectar intentos de XSS
    private static readonly Regex[] XssPatterns =
    {
        new(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new(@"javascript:", RegexOptions.IgnoreCase),
        new(@"vbscript:", RegexOptions.IgnoreCase),
        new(@"on\w+\s*=", RegexOptions.IgnoreCase),
        new(@"<iframe[^>]*>.*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new(@"<object[^>]*>.*?</object>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new(@"<embed[^>]*>.*?</embed>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new(@"<applet[^>]*>.*?</applet>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new(@"expression\s*\(", RegexOptions.IgnoreCase),
        new(@"@import", RegexOptions.IgnoreCase),
        new(@"<meta[^>]*http-equiv", RegexOptions.IgnoreCase)
    };

    // Patrones para detectar intentos de SQL Injection
    private static readonly Regex[] SqlInjectionPatterns =
    {
        new(@"(\b(select|insert|update|delete|drop|create|alter|exec|execute|sp_|xp_)\b)", RegexOptions.IgnoreCase),
        new(@"(\bunion\b.*\bselect\b)", RegexOptions.IgnoreCase),
        new(@"(\bor\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase),
        new(@"(\band\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase),
        new(@"('\s*(or|and)\s*')", RegexOptions.IgnoreCase),
        new(@"(;\s*(select|insert|update|delete|drop))", RegexOptions.IgnoreCase),
        new(@"(\bwhere\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase),
        new(@"(--|\#|/\*|\*/)", RegexOptions.IgnoreCase),
        new(@"(\bchar\s*\(\s*\d+\s*\))", RegexOptions.IgnoreCase),
        new(@"(\bhex\s*\(\s*)", RegexOptions.IgnoreCase)
    };

    // Caracteres peligrosos comunes
    private static readonly char[] DangerousChars =
    {
        '<', '>', '"', '\'', '&', '\0', '\r', '\n'
    };

    // Patrones para detectar intentos de Path Traversal
    private static readonly Regex[] PathTraversalPatterns =
    {
        new(@"\.\./", RegexOptions.IgnoreCase),
        new(@"\.\.\\", RegexOptions.IgnoreCase),
        new(@"%2e%2e%2f", RegexOptions.IgnoreCase),
        new(@"%2e%2e%5c", RegexOptions.IgnoreCase),
        new(@"\.\.%2f", RegexOptions.IgnoreCase),
        new(@"\.\.%5c", RegexOptions.IgnoreCase)
    };

    public static IRuleBuilder<T, string> NoXssContent<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !XssPatterns.Any(pattern => pattern.IsMatch(value));
        }).WithMessage("El contenido contiene scripts o código potencialmente peligroso");
    }

    public static IRuleBuilder<T, string> NoSqlInjection<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !SqlInjectionPatterns.Any(pattern => pattern.IsMatch(value));
        }).WithMessage("El contenido contiene patrones de inyección SQL potencialmente peligrosos");
    }

    public static IRuleBuilder<T, string> NoPathTraversal<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !PathTraversalPatterns.Any(pattern => pattern.IsMatch(value));
        }).WithMessage("El contenido contiene patrones de path traversal peligrosos");
    }

    public static IRuleBuilder<T, string> SafeText<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NoXssContent()
            .NoSqlInjection()
            .NoPathTraversal()
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                return !DangerousChars.Any(value.Contains);
            }).WithMessage("El contenido contiene caracteres potencialmente peligrosos");
    }

    public static IRuleBuilder<T, string> SafeHtml<T>(this IRuleBuilder<T, string> ruleBuilder,
        HashSet<string>? allowedTags = null)
    {
        allowedTags ??= new HashSet<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" };

        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            // Detectar tags HTML
            var tagPattern = new Regex(@"<(/?)(\w+)[^>]*>", RegexOptions.IgnoreCase);
            var matches = tagPattern.Matches(value);

            foreach (Match match in matches)
            {
                var tagName = match.Groups[2].Value.ToLowerInvariant();
                if (!allowedTags.Contains(tagName)) return false;
            }

            // Verificar que no contenga scripts peligrosos
            return !XssPatterns.Any(pattern => pattern.IsMatch(value));
        }).WithMessage($"El HTML contiene tags no permitidos. Tags permitidos: {string.Join(", ", allowedTags)}");
    }

    public static IRuleBuilder<T, string> ValidFileName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var reservedNames = new[]
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            // Verificar caracteres inválidos
            if (invalidChars.Any(value.Contains))
                return false;

            // Verificar nombres reservados de Windows
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(value);
            if (reservedNames.Contains(nameWithoutExtension.ToUpperInvariant()))
                return false;

            // Verificar que no termine con punto o espacio
            if (value.EndsWith('.') || value.EndsWith(' '))
                return false;

            return true;
        }).WithMessage("El nombre de archivo contiene caracteres inválidos o es un nombre reservado");
    }

    public static IRuleBuilder<T, string> ValidUrl<T>(this IRuleBuilder<T, string> ruleBuilder,
        bool allowedLocalhost = false)
    {
        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            // Solo permitir HTTP y HTTPS
            if (uri.Scheme != "http" && uri.Scheme != "https")
                return false;

            // Verificar si localhost está permitido
            if (!allowedLocalhost &&
                (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.StartsWith("192.168.")))
                return false;

            return true;
        }).WithMessage("La URL no es válida o no está permitida");
    }

    public static IRuleBuilder<T, string> NoMaliciousPatterns<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        var maliciousPatterns = new[]
        {
            @"eval\s*\(",
            @"setTimeout\s*\(",
            @"setInterval\s*\(",
            @"Function\s*\(",
            @"document\.cookie",
            @"document\.write",
            @"window\.location",
            @"document\.location",
            @"innerHTML",
            @"outerHTML"
        };

        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !maliciousPatterns.Any(pattern =>
                Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
        }).WithMessage("El contenido contiene patrones de código potencialmente maliciosos");
    }

    public static IRuleBuilder<T, string> SafeLength<T>(this IRuleBuilder<T, string> ruleBuilder,
        int maxLength = 10000)
    {
        return ruleBuilder
            .Length(0, maxLength)
            .WithMessage($"El texto es demasiado largo. Máximo permitido: {maxLength} caracteres");
    }

    public static IRuleBuilder<T, string> AlphanumericWithSpecialChars<T>(this IRuleBuilder<T, string> ruleBuilder,
        string allowedSpecialChars = " .-_@")
    {
        return ruleBuilder.Must(value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return value.All(c => char.IsLetterOrDigit(c) || allowedSpecialChars.Contains(c));
        }).WithMessage(
            $"Solo se permiten caracteres alfanuméricos y estos caracteres especiales: {allowedSpecialChars}");
    }
}

// Extensiones para validaciones de seguridad específicas por campo
public static class FieldSecurityValidators
{
    public static IRuleBuilder<T, string> SafeDisplayName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .SafeText()
            .SafeLength(100)
            .AlphanumericWithSpecialChars(" .-'");
    }

    public static IRuleBuilder<T, string> SafeDescription<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .SafeHtml(new HashSet<string> { "b", "i", "em", "strong", "p", "br" })
            .SafeLength(2000);
    }

    public static IRuleBuilder<T, string> SafeSearchTerm<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .SafeText()
            .SafeLength(500);
    }

    public static IRuleBuilder<T, string> SafeComment<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NoXssContent()
            .NoMaliciousPatterns()
            .SafeLength(5000);
    }
}