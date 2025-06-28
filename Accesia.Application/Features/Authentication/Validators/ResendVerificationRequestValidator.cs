using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Features.Authentication.Validators;

public class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido")
            .Must(BeValidEmail).WithMessage("El email no tiene un formato válido");
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
}
