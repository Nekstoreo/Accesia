using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        
        builder.HasKey(x => x.Id);
        
        // Propiedades básicas
        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.PermissionId).IsRequired();
        builder.Property(x => x.GrantedAt).IsRequired();
        builder.Property(x => x.GrantedBy).IsRequired();
        builder.Property(x => x.IsInherited).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Conditions).HasColumnType("jsonb");
        
        // Índices para performance
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => x.PermissionId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.GrantedAt);
        
        // Relaciones
        builder.HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.GrantedByUser)
            .WithMany()
            .HasForeignKey(x => x.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
} 