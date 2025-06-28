using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accesia.Domain.Entities;

namespace Accesia.Infrastructure.Data.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistories");

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ph => ph.UserId)
            .IsRequired();

        builder.Property(ph => ph.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ph => ph.PasswordChangedAt)
            .IsRequired();

        builder.HasOne(ph => ph.User)
            .WithMany(u => u.PasswordHistories)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ph => ph.UserId);
        builder.HasIndex(ph => ph.PasswordChangedAt);
    }
} 