using Accesia.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Authentication.Validators;

public class RequestPasswordResetRequestValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(256)
            .WithMessage("El email no puede exceder 256 caracteres");
    }
}