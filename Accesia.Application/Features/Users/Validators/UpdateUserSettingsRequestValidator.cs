using FluentValidation;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Validators;

public class UpdateUserSettingsRequestValidator : AbstractValidator<UpdateUserSettingsRequest>
{
    public UpdateUserSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido.");

        RuleFor(x => x)
            .Must(HaveAtLeastOneSettingSection)
            .WithMessage("Debe especificar al menos una sección de configuración para actualizar.");

        // Validaciones para LocalizationSettings
        When(x => x.LocalizationSettings != null, () =>
        {
            RuleFor(x => x.LocalizationSettings!.PreferredLanguage)
                .Matches(@"^[a-z]{2}(-[A-Z]{2})?$")
                .When(x => !string.IsNullOrEmpty(x.LocalizationSettings!.PreferredLanguage))
                .WithMessage("El formato del idioma debe ser 'es' o 'es-ES'.");

            RuleFor(x => x.LocalizationSettings!.TimeZone)
                .Must(BeValidTimeZone)
                .When(x => !string.IsNullOrEmpty(x.LocalizationSettings!.TimeZone))
                .WithMessage("La zona horaria especificada no es válida.");

            RuleFor(x => x.LocalizationSettings!.DateFormat)
                .Must(BeValidDateFormat)
                .When(x => !string.IsNullOrEmpty(x.LocalizationSettings!.DateFormat))
                .WithMessage("El formato de fecha debe ser 'dd/MM/yyyy', 'MM/dd/yyyy' o 'yyyy-MM-dd'.");

            RuleFor(x => x.LocalizationSettings!.TimeFormat)
                .Must(BeValidTimeFormat)
                .When(x => !string.IsNullOrEmpty(x.LocalizationSettings!.TimeFormat))
                .WithMessage("El formato de tiempo debe ser '12h' o '24h'.");
        });

        // Validaciones para SecuritySettings
        When(x => x.SecuritySettings != null, () =>
        {
            RuleFor(x => x.SecuritySettings!.SessionTimeoutMinutes)
                .InclusiveBetween(5, 480)
                .When(x => x.SecuritySettings!.SessionTimeoutMinutes.HasValue)
                .WithMessage("El tiempo de sesión debe estar entre 5 minutos y 8 horas.");
        });
    }

    private static bool HaveAtLeastOneSettingSection(UpdateUserSettingsRequest request)
    {
        return request.NotificationSettings != null ||
               request.PrivacySettings != null ||
               request.LocalizationSettings != null ||
               request.SecuritySettings != null;
    }

    private static bool BeValidTimeZone(string? timeZone)
    {
        if (string.IsNullOrEmpty(timeZone))
            return true;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidDateFormat(string? dateFormat)
    {
        if (string.IsNullOrEmpty(dateFormat))
            return true;

        var validFormats = new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
        return validFormats.Contains(dateFormat);
    }

    private static bool BeValidTimeFormat(string? timeFormat)
    {
        if (string.IsNullOrEmpty(timeFormat))
            return true;

        return timeFormat is "12h" or "24h";
    }
} 