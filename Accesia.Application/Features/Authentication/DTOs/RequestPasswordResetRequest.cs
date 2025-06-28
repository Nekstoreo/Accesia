using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public class RequestPasswordResetRequest
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    [MaxLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    public string? ClientIpAddress { get; set; }
} 