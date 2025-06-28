using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public record RefreshTokenRequest
{
    [Required(ErrorMessage = "El refresh token es requerido")]
    public required string RefreshToken { get; init; }
} 