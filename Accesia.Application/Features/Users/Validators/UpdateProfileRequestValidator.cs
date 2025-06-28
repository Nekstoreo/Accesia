using Accesia.Application.Features.Users.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Users.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("El nombre es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede tener más de 100 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$")
            .WithMessage("El nombre solo puede contener letras y espacios");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("El apellido es requerido")
            .MaximumLength(100)
            .WithMessage("El apellido no puede tener más de 100 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$")
            .WithMessage("El apellido solo puede contener letras y espacios");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .WithMessage("El número de teléfono no puede tener más de 20 caracteres")
            .Matches(@"^[\+]?[0-9\s\-\(\)]+$")
            .WithMessage("El número de teléfono tiene un formato inválido")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .WithMessage("El idioma preferido es requerido")
            .MaximumLength(10)
            .WithMessage("El idioma preferido no puede tener más de 10 caracteres")
            .Must(BeValidLanguageCode)
            .WithMessage("El código de idioma no es válido");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .WithMessage("La zona horaria es requerida")
            .MaximumLength(50)
            .WithMessage("La zona horaria no puede tener más de 50 caracteres")
            .Must(BeValidTimeZone)
            .WithMessage("La zona horaria no es válida");
    }

    private static bool BeValidLanguageCode(string languageCode)
    {
        var validLanguages = new[] { "es", "en", "fr", "pt", "it", "de" };
        return validLanguages.Contains(languageCode.ToLower());
    }

    private static bool BeValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            // Algunos formatos alternativos comunes
            var commonTimeZones = new[]
            {
                "America/Bogota", "America/New_York", "America/Los_Angeles",
                "Europe/Madrid", "Europe/London", "Asia/Tokyo"
            };
            return commonTimeZones.Contains(timeZone);
        }
    }
}