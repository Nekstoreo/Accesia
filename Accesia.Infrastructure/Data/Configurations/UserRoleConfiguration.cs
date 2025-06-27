using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        
        builder.HasKey(x => x.Id);
        
        // Propiedades básicas
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.AssignedAt).IsRequired();
        builder.Property(x => x.AssignedBy).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.ApprovedAt);
        builder.Property(x => x.Reason).HasMaxLength(500);
        
        // Índices para performance
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.ExpiresAt);
        
        // Relaciones
        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.AssignedByUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedBy)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
} 