using System.Text.RegularExpressions;

namespace Accesia.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public Email(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío.", nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!IsValidEmail(normalizedEmail))
            throw new ArgumentException("El formato del email no es válido.", nameof(email));

        Value = normalizedEmail;
    }

    private bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }

    public static Email Create(string email)
    {
        return new Email(email);
    }



    public override string ToString() => Value;
}
