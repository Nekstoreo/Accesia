using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Features.Authentication.Validators;

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido")
            .Must(BeValidEmail).WithMessage("El email no tiene un formato válido");
    
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .Must(BeStrongPassword).WithMessage("La contraseña debe tener al menos 8 caracteres, incluir mayúsculas, minúsculas, números y caracteres especiales");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .Length(1, 100).WithMessage("El nombre debe tener entre 1 y 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio")
            .Length(1, 100).WithMessage("El apellido debe tener entre 1 y 100 caracteres");

        RuleFor(x => x.PhoneNumber)
            .Length(10, 15).When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("El teléfono debe tener entre 10 y 15 caracteres");
    }

    private bool BeValidEmail(string email)
    {
        try
        {
            var emailVO = new Email(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool BeStrongPassword(string password)
    {
        return Password.IsStrongPassword(password);
    }
}