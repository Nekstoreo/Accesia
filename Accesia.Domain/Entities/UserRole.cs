using Accesia.Domain.Common;

namespace Accesia.Domain.Entities;

public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid AssignedBy { get; set; }
    public bool IsActive { get; set; }

    // Propiedades de navegación
    public required User User { get; set; }
    public required Role Role { get; set; }
    public required User AssignedByUser { get; set; }
}