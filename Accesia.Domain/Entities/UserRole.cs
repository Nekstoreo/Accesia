using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Reason { get; set; }

    // Propiedades de navegación
    public required User User { get; set; }
    public required Role Role { get; set; }
    public required User AssignedByUser { get; set; }
    public User? ApprovedByUser { get; set; }
}