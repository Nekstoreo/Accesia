using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class ConfirmEmailChangeRequest
{
    [Required] [EmailAddress] public required string NewEmail { get; set; } = string.Empty;

    [Required] public required string VerificationToken { get; set; } = string.Empty;
}