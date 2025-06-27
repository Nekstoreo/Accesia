using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        
        builder.HasKey(x => x.Id);
        
        // Propiedades básicas
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Resource).IsRequired();
        builder.Property(x => x.Action).IsRequired();
        builder.Property(x => x.Scope).IsRequired();
        builder.Property(x => x.IsSystemPermission).IsRequired().HasDefaultValue(false);
        
        // Propiedades adicionales
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.RequiresApproval).HasDefaultValue(false);
        builder.Property(x => x.RiskLevel).HasDefaultValue(5);
        builder.Property(x => x.Conditions).HasColumnType("jsonb");
        builder.Property(x => x.ValidFrom);
        builder.Property(x => x.ValidUntil);
        
        // Índice único para Name
        builder.HasIndex(x => x.Name).IsUnique();
        
        // Índice compuesto para Resource + Action + Scope
        builder.HasIndex(x => new { x.Resource, x.Action, x.Scope }).IsUnique();
        
        // Índices adicionales para consultas frecuentes
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsSystemPermission);
        builder.HasIndex(x => x.IsActive);
    }
} 