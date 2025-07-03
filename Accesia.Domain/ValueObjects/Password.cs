using System.Text.RegularExpressions;

namespace Accesia.Domain.ValueObjects;

public record Password
{
    public string Value { get; }

    // Solo permite letras mayúsculas, minúsculas, números, caracteres especiales y longitud mínima de 8 caracteres
    private static readonly Regex PasswordRegex = new Regex(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]).{8,}$", 
        RegexOptions.Compiled
    );

    public Password(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        if (!IsStrongPassword(password))
            throw new ArgumentException("La contraseña no cumple con los requisitos de seguridad.", nameof(password));

        Value = password;
    }

    public static bool IsStrongPassword(string password)
    {
        return PasswordRegex.IsMatch(password);
    }

    public static bool ValidatePassword(string password)
    {
        return IsStrongPassword(password);
    }

    public override string ToString() => new string('*', Value.Length);
}
