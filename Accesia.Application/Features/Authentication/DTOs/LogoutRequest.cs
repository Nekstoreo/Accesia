using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public class LogoutRequest
{
    [Required(ErrorMessage = "El token de sesión es requerido")]
    public required string SessionToken { get; set; }
} 