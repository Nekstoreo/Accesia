using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class RequestAccountDeletionRequest
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public required string CurrentPassword { get; set; }

    [MaxLength(500, ErrorMessage = "La razón no puede exceder 500 caracteres")]
    public string? Reason { get; set; }

    [Required(ErrorMessage = "La confirmación es requerida")]
    public bool ConfirmDeletion { get; set; }
}