using FluentValidation;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Validators;

public class CancelAccountDeletionRequestValidator : AbstractValidator<CancelAccountDeletionRequest>
{
    public CancelAccountDeletionRequestValidator()
    {
        RuleFor(x => x.CancellationToken)
            .NotEmpty()
            .WithMessage("El token de cancelación es requerido")
            .Length(32, 256)
            .WithMessage("El token de cancelación tiene un formato inválido");
    }
} 