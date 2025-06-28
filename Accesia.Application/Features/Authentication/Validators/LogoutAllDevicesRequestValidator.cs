using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Validators;

public class LogoutAllDevicesRequestValidator : AbstractValidator<LogoutAllDevicesRequest>
{
    public LogoutAllDevicesRequestValidator()
    {
        RuleFor(x => x.CurrentSessionToken)
            .NotEmpty()
            .WithMessage("El token de sesión actual es requerido")
            .MinimumLength(32)
            .WithMessage("El token de sesión debe tener al menos 32 caracteres")
            .MaximumLength(100)
            .WithMessage("El token de sesión no puede tener más de 100 caracteres");
    }
} 