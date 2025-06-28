using System.ComponentModel.DataAnnotations;
namespace Accesia.Application.Features.Authentication.DTOs;

public class VerifyEmailRequest
{
    [Required]
    [MinLength(32)]
    [MaxLength(128)]
    public required string Token { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
}