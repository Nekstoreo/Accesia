using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        
        builder.HasKey(x => x.Id);
        
        // Propiedades básicas
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsSystemRole).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        
        // Índices únicos
        builder.HasIndex(x => x.Name).IsUnique();
        
        // Jerarquía de roles (auto-referencia)
        builder.HasOne(x => x.ParentRole)
            .WithMany(x => x.ChildRoles)
            .HasForeignKey(x => x.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Propiedades organizacionales
        builder.Property(x => x.OrganizationId);
        builder.Property(x => x.DepartmentId);
        builder.Property(x => x.Priority).HasDefaultValue(5);
        builder.Property(x => x.Level).HasDefaultValue(0);
        
        // Configuración temporal
        builder.Property(x => x.IsTemporary).HasDefaultValue(false);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RequiresApproval).HasDefaultValue(false);
        builder.Property(x => x.ApprovalWorkflow).HasMaxLength(1000);
        
    }
} 