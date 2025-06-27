using System.ComponentModel.DataAnnotations;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Common.Validators;

public class StrongPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        var password = value as string;
        if (string.IsNullOrEmpty(password)) return true;

        return Password.IsStrongPassword(password);
    }

    public override string FormatErrorMessage(string name)
    {
        return "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial";
    }
}