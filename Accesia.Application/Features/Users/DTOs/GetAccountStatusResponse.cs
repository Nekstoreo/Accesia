using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Users.DTOs;

public class GetAccountStatusResponse
{
    public Guid UserId { get; set; }
    public UserStatus CurrentStatus { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public bool IsAccountLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool CanLogin { get; set; }
    public bool CanPerformAction { get; set; }
    public bool RequiresReactivation { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<UserStatus> AllowedTransitions { get; set; } = new();
}