using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public record LoginRequest
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "La contraseña es requerida")]
    public required string Password { get; init; }

    public bool RememberMe { get; init; } = false;
    
    public string? DeviceName { get; init; }
} 