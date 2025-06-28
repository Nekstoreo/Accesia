using Accesia.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Authentication.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El refresh token es requerido")
            .MinimumLength(10)
            .WithMessage("El refresh token no es válido");
    }
}