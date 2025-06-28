using System.ComponentModel.DataAnnotations;

namespace Accesia.Application.Features.Users.DTOs;

public class ChangeEmailRequest
{
    [Required] [EmailAddress] public required string NewEmail { get; set; } = string.Empty;

    [Required] public required string CurrentPassword { get; set; } = string.Empty;

    public string? Reason { get; set; }
}