using FluentValidation;
using Accesia.Application.Features.Users.DTOs;

namespace Accesia.Application.Features.Users.Validators;

public class RequestAccountDeletionRequestValidator : AbstractValidator<RequestAccountDeletionRequest>
{
    public RequestAccountDeletionRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contraseña actual es requerida")
            .MinimumLength(1)
            .WithMessage("Debe proporcionar la contraseña actual");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("La razón no puede exceder 500 caracteres");

        RuleFor(x => x.ConfirmDeletion)
            .Equal(true)
            .WithMessage("Debe confirmar que desea eliminar su cuenta");
    }
} 