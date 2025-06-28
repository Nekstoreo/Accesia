using Accesia.Domain.Entities;
using Accesia.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accesia.Infrastructure.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        // Configuración de tabla
        builder.ToTable("UserSettings");

        // Configuración de clave primaria
        builder.HasKey(us => us.Id);

        // Configuración de propiedades
        builder.Property(us => us.UserId)
            .IsRequired();

        // Preferencias de notificación
        builder.Property(us => us.EmailNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.SmsNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.PushNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.InAppNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        // Configuraciones específicas por tipo de notificación
        builder.Property(us => us.SecurityAlertsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.LoginActivityNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.PasswordChangeNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.AccountUpdateNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.SystemAnnouncementsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.DeviceActivityNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        // Configuraciones de privacidad
        builder.Property(us => us.ProfileVisibility)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PrivacyLevel.Private);

        builder.Property(us => us.ShowLastLoginTime)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.ShowOnlineStatus)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.AllowDataCollection)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.AllowMarketingEmails)
            .IsRequired()
            .HasDefaultValue(false);

        // Configuraciones regionales y de localización
        builder.Property(us => us.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("es");

        builder.Property(us => us.TimeZone)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("America/Bogota");

        builder.Property(us => us.DateFormat)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("dd/MM/yyyy");

        builder.Property(us => us.TimeFormat)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("24h");

        // Configuraciones de seguridad
        builder.Property(us => us.TwoFactorAuthEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.RequirePasswordChangeOn2FADisable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.LogoutOnPasswordChange)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.SessionTimeoutMinutes)
            .IsRequired()
            .HasDefaultValue(60);

        // Configuración de relaciones
        builder.HasOne(us => us.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de índices
        builder.HasIndex(us => us.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserSettings_UserId");

        // Configuración de auditoría (heredada de AuditableEntity)
        builder.Property(us => us.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(us => us.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}