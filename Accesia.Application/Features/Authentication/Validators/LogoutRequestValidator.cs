using Accesia.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Authentication.Validators;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty()
            .WithMessage("El token de sesión es requerido")
            .MinimumLength(32)
            .WithMessage("El token de sesión debe tener al menos 32 caracteres")
            .MaximumLength(100)
            .WithMessage("El token de sesión no puede tener más de 100 caracteres");
    }
}