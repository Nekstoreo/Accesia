using System.Text.RegularExpressions;

namespace Accesia.Domain.ValueObjects;

public record Password
{
    // Solo permite letras mayúsculas, minúsculas, números, caracteres especiales y longitud mínima de 8 caracteres
    private static readonly Regex PasswordRegex = new(
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

    public string Value { get; }

    public static bool IsStrongPassword(string password)
    {
        return PasswordRegex.IsMatch(password);
    }

    public static bool ValidatePassword(string password)
    {
        return IsStrongPassword(password);
    }

    public override string ToString()
    {
        return new string('*', Value.Length);
    }
}