using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class CancelAccountDeletionRequest
{
    [Required(ErrorMessage = "El token de cancelación es requerido")]
    public required string CancellationToken { get; set; }
} 