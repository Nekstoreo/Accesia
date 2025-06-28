using Accesia.Application.Features.Users.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Users.Validators;

public class ConfirmEmailChangeRequestValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .WithMessage("El nuevo email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(320)
            .WithMessage("El email no puede tener más de 320 caracteres");

        RuleFor(x => x.VerificationToken)
            .NotEmpty()
            .WithMessage("El token de verificación es requerido")
            .Length(32, 128)
            .WithMessage("El token de verificación tiene un formato inválido");
    }
}