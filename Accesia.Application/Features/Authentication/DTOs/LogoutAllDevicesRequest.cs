using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public class LogoutAllDevicesRequest
{
    [Required(ErrorMessage = "El token de sesión actual es requerido")]
    public required string CurrentSessionToken { get; set; }
} 