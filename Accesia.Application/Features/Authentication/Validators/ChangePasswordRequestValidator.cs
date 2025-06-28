using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contraseña actual es requerida");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("La nueva contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("La contraseña debe contener al menos una letra minúscula, una mayúscula, un número y un carácter especial")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("La nueva contraseña debe ser diferente a la contraseña actual");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.NewPassword)
            .WithMessage("Las contraseñas no coinciden");
    }
} 