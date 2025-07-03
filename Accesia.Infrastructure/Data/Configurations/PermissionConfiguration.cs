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
        builder.Property(x => x.Scope).IsRequired();
        builder.Property(x => x.IsSystemPermission).IsRequired().HasDefaultValue(false);
        
        // Propiedades adicionales
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        
        // Índice único para Name
        builder.HasIndex(x => x.Name).IsUnique();
        
        // Índices adicionales para consultas frecuentes
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsSystemPermission);
        builder.HasIndex(x => x.IsActive);
    }
} 