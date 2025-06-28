using System.ComponentModel.DataAnnotations;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.DTOs;

public class ChangeAccountStatusRequest
{
    [Required] public Guid UserId { get; set; }

    [Required]
    [EnumDataType(typeof(UserStatus))]
    public UserStatus NewStatus { get; set; }

    [MaxLength(500)] public string? Reason { get; set; }
}