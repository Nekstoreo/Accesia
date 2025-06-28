using System.ComponentModel.DataAnnotations;
using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class PasswordHistory : AuditableEntity
{
    private PasswordHistory()
    {
    }

    public PasswordHistory(Guid userId, string passwordHash)
    {
        UserId = userId;
        PasswordHash = passwordHash;
        PasswordChangedAt = DateTime.UtcNow;
    }

    [Required] public Guid UserId { get; set; }

    [Required] public string PasswordHash { get; set; } = string.Empty;

    [Required] public DateTime PasswordChangedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public static PasswordHistory Create(Guid userId, string passwordHash)
    {
        return new PasswordHistory(userId, passwordHash);
    }
}