using Accesia.Domain.Enums;

namespace Accesia.Domain.ValueObjects;

public class DeviceInfo
{
    public string UserAgent { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;

    // Constructor sin parámetros para EF Core
    public DeviceInfo() { }

    public DeviceInfo(
        string userAgent,
        DeviceType deviceType,
        string browser,
        string browserVersion,
        string operatingSystem,
        string deviceFingerprint)
    {
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        DeviceType = deviceType;
        Browser = browser ?? "Unknown";
        BrowserVersion = browserVersion ?? "Unknown";
        OperatingSystem = operatingSystem ?? "Unknown";
        DeviceFingerprint = deviceFingerprint ?? throw new ArgumentNullException(nameof(deviceFingerprint));
    }

    public static DeviceInfo CreateFromUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent no puede estar vacío", nameof(userAgent));

        // Análisis básico del user agent (en una implementación real usarías una librería especializada)
        var deviceType = DetectDeviceType(userAgent);
        var browser = DetectBrowser(userAgent);
        var browserVersion = DetectBrowserVersion(userAgent);
        var operatingSystem = DetectOperatingSystem(userAgent);
        var fingerprint = GenerateFingerprint(userAgent);

        return new DeviceInfo(userAgent, deviceType, browser, browserVersion, operatingSystem, fingerprint);
    }

    private static DeviceType DetectDeviceType(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return DeviceType.Mobile;
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return DeviceType.Tablet;
        if (ua.Contains("bot") || ua.Contains("crawler") || ua.Contains("spider"))
            return DeviceType.Bot;
        
        return DeviceType.Desktop;
    }

    private static string DetectBrowser(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("chrome")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari")) return "Safari";
        if (ua.Contains("edge")) return "Edge";
        if (ua.Contains("opera")) return "Opera";
        
        return "Unknown";
    }

    private static string DetectBrowserVersion(string userAgent)
    {
        // Implementación básica - en producción usarías una librería más robusta
        return "Unknown";
    }

    private static string DetectOperatingSystem(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("mac")) return "macOS";
        if (ua.Contains("linux")) return "Linux";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("ios")) return "iOS";
        
        return "Unknown";
    }

    private static string GenerateFingerprint(string userAgent)
    {
        // Genera un hash del user agent para identificación
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userAgent));
        return Convert.ToHexString(hash)[..16]; // Primeros 16 caracteres
    }
}