using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;

namespace Accesia.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(x => x.Id);
        
        // Configuración del value object Email
        builder.Property(x => x.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(320)
            .IsRequired();
            
        builder.HasIndex(x => x.Email).IsUnique();
        
        // Propiedades básicas
        builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        
        // Campos opcionales
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        
        // Índices para performance
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.LastLoginAt);
        
        // Relación con Sessions
        builder.HasMany(x => x.Sessions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Relación con UserRoles
        builder.HasMany(x => x.UserRoles)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 