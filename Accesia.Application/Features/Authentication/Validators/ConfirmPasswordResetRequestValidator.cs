using Accesia.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Authentication.Validators;

public class ConfirmPasswordResetRequestValidator : AbstractValidator<ConfirmPasswordResetRequest>
{
    public ConfirmPasswordResetRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("El token es requerido")
            .MinimumLength(20)
            .WithMessage("El token debe tener al menos 20 caracteres");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("La nueva contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage(
                "La contraseña debe contener al menos una letra minúscula, una mayúscula, un número y un carácter especial");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.NewPassword)
            .WithMessage("Las contraseñas no coinciden");
    }
}