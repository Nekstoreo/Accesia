using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class ConfirmAccountDeletionRequest
{
    [Required(ErrorMessage = "El token de confirmación es requerido")]
    public required string DeletionToken { get; set; }

    [Required(ErrorMessage = "La confirmación final es requerida")]
    public bool FinalConfirmation { get; set; }
} 