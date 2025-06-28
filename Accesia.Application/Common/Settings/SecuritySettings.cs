namespace Accesia.Application.Common.Settings;

public class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    public RateLimitSettings RateLimit { get; set; } = new();
    public InputValidationSettings InputValidation { get; set; } = new();
    public SecurityAuditSettings SecurityAudit { get; set; } = new();
    public AlertingSettings Alerting { get; set; } = new();
}

public class RateLimitSettings
{
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new()
    {
        ["LoginAttempt"] = new RateLimitPolicy
        {
            MaxAttempts = 5,
            WindowMinutes = 15,
            Type = "SlidingWindow",
            BlockDurationMinutes = 30,
            Segments = 3
        },
        ["PasswordReset"] = new RateLimitPolicy
        {
            MaxAttempts = 3,
            WindowMinutes = 60,
            Type = "FixedWindow",
            BlockDurationMinutes = 120
        },
        ["UserRegistration"] = new RateLimitPolicy
        {
            MaxAttempts = 3,
            WindowMinutes = 1440, // 24 horas
            Type = "FixedWindow",
            BlockDurationMinutes = 1440
        },
        ["EmailVerification"] = new RateLimitPolicy
        {
            MaxAttempts = 5,
            WindowMinutes = 60,
            Type = "TokenBucket",
            TokensPerPeriod = 5,
            ReplenishmentPeriodMinutes = 60
        },
        ["ProfileUpdate"] = new RateLimitPolicy
        {
            MaxAttempts = 10,
            WindowMinutes = 60,
            Type = "TokenBucket",
            TokensPerPeriod = 10,
            ReplenishmentPeriodMinutes = 60
        },
        ["EmailChange"] = new RateLimitPolicy
        {
            MaxAttempts = 1,
            WindowMinutes = 1440, // 24 horas
            Type = "FixedWindow",
            BlockDurationMinutes = 1440
        },
        ["AccountDeletion"] = new RateLimitPolicy
        {
            MaxAttempts = 1,
            WindowMinutes = 10080, // 7 días
            Type = "FixedWindow",
            BlockDurationMinutes = 10080
        },
        ["SuspiciousActivity"] = new RateLimitPolicy
        {
            MaxAttempts = 3,
            WindowMinutes = 5,
            Type = "SlidingWindow",
            BlockDurationMinutes = 60,
            Segments = 5
        }
    };

    public bool EnableUserSpecificLimits { get; set; } = true;
    public bool EnableIpSpecificLimits { get; set; } = true;
    public bool EnableEndpointSpecificLimits { get; set; } = true;
    public bool LogRateLimitViolations { get; set; } = true;
}

public class RateLimitPolicy
{
    public int MaxAttempts { get; set; }
    public int WindowMinutes { get; set; }
    public string Type { get; set; } = "FixedWindow"; // FixedWindow, SlidingWindow, TokenBucket
    public int BlockDurationMinutes { get; set; }
    public int Segments { get; set; } = 1; // Para SlidingWindow
    public int TokensPerPeriod { get; set; } = 1; // Para TokenBucket
    public int ReplenishmentPeriodMinutes { get; set; } = 60; // Para TokenBucket
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}

public class InputValidationSettings
{
    public bool EnableXssProtection { get; set; } = true;
    public bool EnableSqlInjectionProtection { get; set; } = true;
    public bool EnableCsrfProtection { get; set; } = true;
    public bool StrictInputSanitization { get; set; } = true;
    public HashSet<string> AllowedHtmlTags { get; set; } = new();
    public HashSet<string> AllowedFileExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
    public int MaxInputLength { get; set; } = 10000;
    public int MaxFileSize { get; set; } = 5242880; // 5MB
    public Dictionary<string, string> FieldWhitelist { get; set; } = new();
}

public class SecurityAuditSettings
{
    public bool EnableDetailedLogging { get; set; } = true;
    public bool LogSuccessfulOperations { get; set; } = true;
    public bool LogFailedOperations { get; set; } = true;
    public bool EnableGeoLocationTracking { get; set; } = false;
    public bool EnableDeviceFingerprinting { get; set; } = true;
    public int LogRetentionDays { get; set; } = 90;
    public HashSet<string> CriticalEvents { get; set; } = new()
    {
        "LoginAttempt",
        "PasswordChange",
        "EmailChange",
        "AccountDeletion",
        "SuspiciousActivity",
        "UnauthorizedAccess",
        "RateLimitExceeded"
    };
    public Dictionary<string, string> SeverityLevels { get; set; } = new()
    {
        ["LoginAttempt"] = "Medium",
        ["PasswordChange"] = "High",
        ["EmailChange"] = "High",
        ["AccountDeletion"] = "Critical",
        ["SuspiciousActivity"] = "Critical",
        ["UnauthorizedAccess"] = "High",
        ["RateLimitExceeded"] = "Medium"
    };
}

public class AlertingSettings
{
    public bool EnableEmailAlerts { get; set; } = false;
    public bool EnableSlackAlerts { get; set; } = false;
    public bool EnableSmsAlerts { get; set; } = false;
    public HashSet<string> AlertOnEventTypes { get; set; } = new()
    {
        "SuspiciousActivity",
        "AccountDeletion",
        "UnauthorizedAccess"
    };
    public HashSet<string> AlertOnSeverities { get; set; } = new() { "Critical", "High" };
    public int AlertThrottleMinutes { get; set; } = 15;
    public List<string> AlertRecipients { get; set; } = new();
    public string SlackWebhookUrl { get; set; } = string.Empty;
    public Dictionary<string, object> CustomAlertSettings { get; set; } = new();
} 