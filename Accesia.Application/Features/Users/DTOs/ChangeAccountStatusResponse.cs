using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.DTOs;

public class ChangeAccountStatusResponse
{
    public Guid UserId { get; set; }
    public UserStatus PreviousStatus { get; set; }
    public UserStatus NewStatus { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}