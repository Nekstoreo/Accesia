using Accesia.Application.Features.Users.DTOs;
using FluentValidation;

namespace Accesia.Application.Features.Users.Validators;

public class ConfirmAccountDeletionRequestValidator : AbstractValidator<ConfirmAccountDeletionRequest>
{
    public ConfirmAccountDeletionRequestValidator()
    {
        RuleFor(x => x.DeletionToken)
            .NotEmpty()
            .WithMessage("El token de confirmación es requerido")
            .Length(32, 256)
            .WithMessage("El token de confirmación tiene un formato inválido");

        RuleFor(x => x.FinalConfirmation)
            .Equal(true)
            .WithMessage("Debe confirmar finalmente que desea eliminar permanentemente su cuenta");
    }
}