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
        
        // Información del dispositivo como JSON
        builder.OwnsOne(x => x.DeviceInfo, deviceBuilder =>
        {
            deviceBuilder.Property(d => d.UserAgent).HasMaxLength(1000);
            deviceBuilder.Property(d => d.DeviceType);
            deviceBuilder.Property(d => d.Browser).HasMaxLength(100);
            deviceBuilder.Property(d => d.BrowserVersion).HasMaxLength(50);
            deviceBuilder.Property(d => d.OperatingSystem).HasMaxLength(100);
            deviceBuilder.Property(d => d.DeviceFingerprint).HasMaxLength(64);
        });
        
        // Información de ubicación como JSON
        builder.OwnsOne(x => x.LocationInfo, locationBuilder =>
        {
            locationBuilder.Property(l => l.IpAddress).HasMaxLength(45).IsRequired();
            locationBuilder.Property(l => l.Country).HasMaxLength(100);
            locationBuilder.Property(l => l.City).HasMaxLength(100);
            locationBuilder.Property(l => l.Region).HasMaxLength(100);
            locationBuilder.Property(l => l.ISP).HasMaxLength(200);
            locationBuilder.Property(l => l.IsVPN);
        });
        
        // Metadatos de seguridad
        builder.Property(x => x.LoginMethod).IsRequired();
        builder.Property(x => x.RiskScore).HasDefaultValue(0);
        
        // Auditoría adicional
        builder.Property(x => x.UserAgent).HasMaxLength(1000);
        builder.Property(x => x.InitialIpAddress).HasMaxLength(45);
        builder.Property(x => x.LastIpAddress).HasMaxLength(45);
        
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