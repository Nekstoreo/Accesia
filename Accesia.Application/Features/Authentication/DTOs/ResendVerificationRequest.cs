using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Authentication.DTOs;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
