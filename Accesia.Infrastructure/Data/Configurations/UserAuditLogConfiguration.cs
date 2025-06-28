using Accesia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accesia.Infrastructure.Data.Configurations;

public class UserAuditLogConfiguration : IEntityTypeConfiguration<UserAuditLog>
{
    public void Configure(EntityTypeBuilder<UserAuditLog> builder)
    {
        builder.ToTable("UserAuditLogs");

        builder.HasKey(x => x.Id);

        // Propiedades básicas
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ActionType).IsRequired();
        builder.Property(x => x.ResourceType).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(1000);
        builder.Property(x => x.NewValue).HasMaxLength(1000);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired(); // IPv6 support
        builder.Property(x => x.UserAgent).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ActionPerformedAt).IsRequired();

        // Índices para performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ActionType);
        builder.HasIndex(x => x.ResourceType);
        builder.HasIndex(x => x.ActionPerformedAt);
        builder.HasIndex(x => new { x.UserId, x.ActionPerformedAt });

        // Relación con User
        builder.HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}