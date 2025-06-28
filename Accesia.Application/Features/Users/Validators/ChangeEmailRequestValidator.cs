using FluentValidation;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Validators;

public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .WithMessage("El nuevo email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(320)
            .WithMessage("El email no puede tener más de 320 caracteres");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contraseña actual es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("La razón no puede tener más de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
} 