using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(256)
            .WithMessage("El email no puede exceder 256 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(1)
            .WithMessage("La contraseña es requerida");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .WithMessage("El nombre del dispositivo no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.DeviceName));
    }
} 