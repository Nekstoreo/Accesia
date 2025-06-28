using System.Text.Json;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accesia.Infrastructure.Data.Configurations;

public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("SecurityAuditLogs");

        builder.HasKey(sal => sal.Id);

        builder.Property(sal => sal.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(sal => sal.UserId)
            .IsRequired(false);

        builder.Property(sal => sal.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sal => sal.EventCategory)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sal => sal.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(sal => sal.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 máximo

        builder.Property(sal => sal.UserAgent)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sal => sal.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sal => sal.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(sal => sal.RequestId)
            .HasMaxLength(100);

        builder.Property(sal => sal.IsSuccessful)
            .IsRequired();

        builder.Property(sal => sal.FailureReason)
            .HasMaxLength(500);

        builder.Property(sal => sal.ResponseStatusCode);

        builder.Property(sal => sal.Severity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sal => sal.OccurredAt)
            .IsRequired();

        // Configuración de DeviceInfo como JSON
        builder.Property(sal => sal.DeviceInfo)
            .IsRequired()
            .HasConversion(
                deviceInfo => JsonSerializer.Serialize(deviceInfo, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<DeviceInfo>(json, (JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        // Configuración de LocationInfo como JSON
        builder.Property(sal => sal.LocationInfo)
            .HasConversion(
                locationInfo => locationInfo == null
                    ? null
                    : JsonSerializer.Serialize(locationInfo, (JsonSerializerOptions?)null),
                json => json == null
                    ? null
                    : JsonSerializer.Deserialize<LocationInfo>(json, (JsonSerializerOptions?)null))
            .HasColumnType("text");

        // Configuración de AdditionalData como JSON
        builder.Property(sal => sal.AdditionalData)
            .IsRequired()
            .HasConversion(
                dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, object>>(json, (JsonSerializerOptions?)null) ??
                        new Dictionary<string, object>())
            .HasColumnType("text");

        // Configurar relación con User
        builder.HasOne(sal => sal.User)
            .WithMany()
            .HasForeignKey(sal => sal.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices para consultas optimizadas
        builder.HasIndex(sal => sal.UserId)
            .HasDatabaseName("IX_SecurityAuditLogs_UserId");

        builder.HasIndex(sal => sal.EventType)
            .HasDatabaseName("IX_SecurityAuditLogs_EventType");

        builder.HasIndex(sal => sal.EventCategory)
            .HasDatabaseName("IX_SecurityAuditLogs_EventCategory");

        builder.HasIndex(sal => sal.OccurredAt)
            .HasDatabaseName("IX_SecurityAuditLogs_OccurredAt");

        builder.HasIndex(sal => sal.IpAddress)
            .HasDatabaseName("IX_SecurityAuditLogs_IpAddress");

        builder.HasIndex(sal => sal.Severity)
            .HasDatabaseName("IX_SecurityAuditLogs_Severity");

        builder.HasIndex(sal => sal.IsSuccessful)
            .HasDatabaseName("IX_SecurityAuditLogs_IsSuccessful");

        // Índice compuesto para consultas comunes
        builder.HasIndex(sal => new { sal.EventType, sal.OccurredAt })
            .HasDatabaseName("IX_SecurityAuditLogs_EventType_OccurredAt");

        builder.HasIndex(sal => new { sal.UserId, sal.OccurredAt })
            .HasDatabaseName("IX_SecurityAuditLogs_UserId_OccurredAt");

        builder.HasIndex(sal => new { sal.Severity, sal.OccurredAt })
            .HasDatabaseName("IX_SecurityAuditLogs_Severity_OccurredAt");
    }
}