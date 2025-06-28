using Accesia.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accesia.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Session> Sessions { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }
    DbSet<PasswordHistory> PasswordHistories { get; set; }
    DbSet<UserAuditLog> UserAuditLogs { get; set; }
    DbSet<UserSettings> UserSettings { get; set; }
    DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}