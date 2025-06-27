namespace Accesia.Domain.Constants;

public static class SystemPermissions
{
    // Gestión de usuarios
    public const string USERS_CREATE = "users:create:global";
    public const string USERS_READ = "users:read:global";
    public const string USERS_UPDATE = "users:update:global";
    public const string USERS_DELETE = "users:delete:global";
    public const string USERS_READ_OWN = "users:read:own";
    public const string USERS_UPDATE_OWN = "users:update:own";
    
    // Gestión de roles
    public const string ROLES_CREATE = "roles:create:global";
    public const string ROLES_READ = "roles:read:global";
    public const string ROLES_UPDATE = "roles:update:global";
    public const string ROLES_DELETE = "roles:delete:global";
    
    // Gestión de sesiones
    public const string SESSIONS_READ = "sessions:read:global";
    public const string SESSIONS_REVOKE = "sessions:revoke:global";
    public const string SESSIONS_READ_OWN = "sessions:read:own";
    public const string SESSIONS_REVOKE_OWN = "sessions:revoke:own";
    
    // Auditoría
    public const string AUDIT_READ = "audit:read:global";
    public const string AUDIT_EXPORT = "audit:export:global";
    
    // Configuración del sistema
    public const string SETTINGS_READ = "settings:read:global";
    public const string SETTINGS_UPDATE = "settings:update:global";
}