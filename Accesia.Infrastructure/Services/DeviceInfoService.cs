using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;

namespace Accesia.Infrastructure.Services;

public class DeviceInfoService : IDeviceInfoService
{
    private readonly IApplicationDbContext _context;

    public DeviceInfoService(IApplicationDbContext context)
    {
        _context = context;
    }

    public DeviceInfo ExtractDeviceInfo(string userAgent, string? additionalInfo = null)
    {
        var deviceType = DetectDeviceType(userAgent);
        var operatingSystem = DetectOperatingSystem(userAgent);
        var browser = DetectBrowser(userAgent);
        var deviceFingerprint = GenerateDeviceFingerprint(userAgent, additionalInfo);

        return new DeviceInfo(
            userAgent: userAgent,
            deviceType: deviceType,
            browser: browser,
            browserVersion: "Unknown",
            operatingSystem: operatingSystem,
            deviceFingerprint: deviceFingerprint
        );
    }

    public LocationInfo ExtractLocationInfo(string ipAddress, string? forwardedFor = null)
    {
        // Obtener la IP real considerando proxies
        var realIpAddress = GetRealIpAddress(ipAddress, forwardedFor);

        return new LocationInfo(
            ipAddress: realIpAddress,
            country: DetermineCountry(realIpAddress),
            city: DetermineCity(realIpAddress),
            region: null,
            isp: null,
            isVPN: false
        );
    }

    public bool IsKnownDevice(Guid userId, DeviceInfo deviceInfo)
    {
        return _context.Sessions
            .Any(s => s.UserId == userId && 
                     s.DeviceInfo.DeviceFingerprint == deviceInfo.DeviceFingerprint);
    }

    public async Task<bool> IsKnownDeviceAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AnyAsync(s => s.UserId == userId && 
                          s.DeviceInfo.DeviceFingerprint == deviceInfo.DeviceFingerprint,
                      cancellationToken);
    }

    private static DeviceType DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return DeviceType.Unknown;

        var userAgentLower = userAgent.ToLowerInvariant();

        if (userAgentLower.Contains("mobile") || 
            userAgentLower.Contains("android") || 
            userAgentLower.Contains("iphone") || 
            userAgentLower.Contains("ipod"))
        {
            return DeviceType.Mobile;
        }

        if (userAgentLower.Contains("tablet") || 
            userAgentLower.Contains("ipad"))
        {
            return DeviceType.Tablet;
        }

        return DeviceType.Desktop;
    }

    private static string DetectOperatingSystem(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var patterns = new Dictionary<string, string>
        {
            { @"Windows NT 10\.0", "Windows 10" },
            { @"Windows NT 6\.3", "Windows 8.1" },
            { @"Windows NT 6\.2", "Windows 8" },
            { @"Windows NT 6\.1", "Windows 7" },
            { @"Windows NT 6\.0", "Windows Vista" },
            { @"Windows NT 5\.1", "Windows XP" },
            { @"Mac OS X", "macOS" },
            { @"iPhone OS|iOS", "iOS" },
            { @"Android", "Android" },
            { @"Linux", "Linux" },
            { @"Ubuntu", "Ubuntu" }
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(userAgent, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }

        return "Unknown";
    }

    private static string DetectBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var patterns = new Dictionary<string, string>
        {
            { @"Chrome/[\d\.]+", "Chrome" },
            { @"Firefox/[\d\.]+", "Firefox" },
            { @"Safari/[\d\.]+", "Safari" },
            { @"Edge/[\d\.]+", "Edge" },
            { @"Opera/[\d\.]+", "Opera" },
            { @"MSIE [\d\.]+", "Internet Explorer" }
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(userAgent, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }

        return "Unknown";
    }

    private static string GenerateDeviceFingerprint(string userAgent, string? additionalInfo)
    {
        var fingerprintData = $"{userAgent}|{additionalInfo ?? ""}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintData));
        return Convert.ToBase64String(hashBytes);
    }

    private static string GetRealIpAddress(string ipAddress, string? forwardedFor)
    {
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Tomar la primera IP de la lista separada por comas
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        return ipAddress;
    }

    private static string? DetermineCountry(string ipAddress)
    {
        // En una implementación real, aquí se usaría un servicio de geolocalización
        // como MaxMind GeoIP2 o similar
        
        // Por ahora, retornar null para indicar que no está implementado
        return null;
    }

    private static string? DetermineCity(string ipAddress)
    {
        // En una implementación real, aquí se usaría un servicio de geolocalización
        // como MaxMind GeoIP2 o similar
        
        // Por ahora, retornar null para indicar que no está implementado
        return null;
    }
}