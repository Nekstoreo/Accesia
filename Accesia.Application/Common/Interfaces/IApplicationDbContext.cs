using Microsoft.EntityFrameworkCore;
using Accesia.Domain.Entities;

namespace Accesia.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Session> Sessions { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
} 