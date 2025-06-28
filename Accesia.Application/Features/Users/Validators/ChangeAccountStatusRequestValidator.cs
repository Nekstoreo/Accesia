using FluentValidation;
using Accesia.Application.Features.Users.DTOs;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.Validators;

public class ChangeAccountStatusRequestValidator : AbstractValidator<ChangeAccountStatusRequest>
{
    public ChangeAccountStatusRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido.");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("El estado especificado no es válido.")
            .Must(BeValidTransitionStatus)
            .WithMessage("El estado especificado no es válido para transiciones manuales.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("La razón no puede exceder 500 caracteres.")
            .NotEmpty()
            .When(x => x.NewStatus == UserStatus.Blocked)
            .WithMessage("La razón es requerida cuando se bloquea una cuenta.");
    }

    private static bool BeValidTransitionStatus(UserStatus status)
    {
        // Solo permitir ciertos estados para transiciones manuales
        return status is UserStatus.Active or UserStatus.Inactive or UserStatus.Blocked or UserStatus.MarkedForDeletion;
    }
} 