using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        
        builder.HasKey(x => x.Id);
        
        // Tokens únicos
        builder.Property(x => x.SessionToken).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RefreshToken).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.SessionToken).IsUnique();
        builder.HasIndex(x => x.RefreshToken).IsUnique();
        
        // Propiedades básicas
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RefreshTokenExpiresAt).IsRequired();
        builder.Property(x => x.LastActivityAt).IsRequired();
        
        // Metadatos de seguridad
        builder.Property(x => x.LoginMethod).IsRequired();
        
        // Índices para performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => new { x.UserId, x.Status });
        
        // Relación con User
        builder.HasOne(x => x.User)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 