namespace Accesia.Application.Common.Exceptions;

public class UserSettingsException : Exception
{
    public string UserId { get; }
    public string SettingName { get; }

    public UserSettingsException(string userId, string settingName, string message) 
        : base(message)
    {
        UserId = userId;
        SettingName = settingName;
    }

    public UserSettingsException(string userId, string settingName, string message, Exception innerException) 
        : base(message, innerException)
    {
        UserId = userId;
        SettingName = settingName;
    }
}

public class InvalidTimeZoneException : UserSettingsException
{
    public string ProvidedTimeZone { get; }

    public InvalidTimeZoneException(string userId, string providedTimeZone) 
        : base(userId, "TimeZone", $"La zona horaria '{providedTimeZone}' no es válida.")
    {
        ProvidedTimeZone = providedTimeZone;
    }
}

public class InvalidLanguageCodeException : UserSettingsException
{
    public string ProvidedLanguageCode { get; }

    public InvalidLanguageCodeException(string userId, string providedLanguageCode) 
        : base(userId, "PreferredLanguage", $"El código de idioma '{providedLanguageCode}' no es válido. Use formato 'es' o 'es-ES'.")
    {
        ProvidedLanguageCode = providedLanguageCode;
    }
}

public class InvalidDateFormatException : UserSettingsException
{
    public string ProvidedFormat { get; }

    public InvalidDateFormatException(string userId, string providedFormat) 
        : base(userId, "DateFormat", $"El formato de fecha '{providedFormat}' no es válido. Use 'dd/MM/yyyy', 'MM/dd/yyyy', 'yyyy-MM-dd' o 'dd-MM-yyyy'.")
    {
        ProvidedFormat = providedFormat;
    }
}

public class InvalidTimeFormatException : UserSettingsException
{
    public string ProvidedFormat { get; }

    public InvalidTimeFormatException(string userId, string providedFormat) 
        : base(userId, "TimeFormat", $"El formato de tiempo '{providedFormat}' no es válido. Use '12h' o '24h'.")
    {
        ProvidedFormat = providedFormat;
    }
}

public class InvalidSessionTimeoutException : UserSettingsException
{
    public int ProvidedMinutes { get; }

    public InvalidSessionTimeoutException(string userId, int providedMinutes) 
        : base(userId, "SessionTimeoutMinutes", $"El tiempo de sesión {providedMinutes} minutos no es válido. Debe estar entre 5 y 480 minutos (8 horas).")
    {
        ProvidedMinutes = providedMinutes;
    }
}

public class ConflictingSecuritySettingsException : UserSettingsException
{
    public string ConflictDescription { get; }

    public ConflictingSecuritySettingsException(string userId, string conflictDescription) 
        : base(userId, "SecuritySettings", $"Configuración de seguridad conflictiva: {conflictDescription}")
    {
        ConflictDescription = conflictDescription;
    }
} 