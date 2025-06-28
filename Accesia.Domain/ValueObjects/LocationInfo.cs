using System.Net;

namespace Accesia.Domain.ValueObjects;

public class LocationInfo
{
    // Constructor sin parámetros para EF Core
    public LocationInfo()
    {
    }

    public LocationInfo(
        string ipAddress,
        string? country = null,
        string? city = null,
        string? region = null,
        string? isp = null,
        bool isVPN = false)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address no puede estar vacía", nameof(ipAddress));

        if (!IsValidIpAddress(ipAddress))
            throw new ArgumentException("IP address no tiene un formato válido", nameof(ipAddress));

        IpAddress = ipAddress;
        Country = country;
        City = city;
        Region = region;
        ISP = isp;
        IsVPN = isVPN;
    }

    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? ISP { get; set; }
    public bool IsVPN { get; set; }

    public static LocationInfo CreateFromIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address no puede estar vacía", nameof(ipAddress));
        // TODO: Implementar geolocalización
        // En una implementación real, aquí consultarías un servicio de geolocalización
        // como MaxMind GeoIP2, IP2Location, etc.
        return new LocationInfo(
            ipAddress,
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown"
        );
    }

    public static LocationInfo CreateLocalhost()
    {
        return new LocationInfo(
            "127.0.0.1",
            "Local",
            "Local",
            "Local",
            "Local"
        );
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    public bool IsLocalAddress()
    {
        return IpAddress == "127.0.0.1" ||
               IpAddress == "::1" ||
               IpAddress.StartsWith("192.168.") ||
               IpAddress.StartsWith("10.") ||
               (IpAddress.StartsWith("172.") && IsPrivateClassB(IpAddress));
    }

    private static bool IsPrivateClassB(string ipAddress)
    {
        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length >= 2)
            {
                var secondOctet = bytes[1];
                return secondOctet >= 16 && secondOctet <= 31;
            }
        }

        return false;
    }

    public string GetDisplayLocation()
    {
        if (IsLocalAddress())
            return "Local";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
        if (!string.IsNullOrWhiteSpace(Region)) parts.Add(Region);
        if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country);

        return parts.Any() ? string.Join(", ", parts) : "Unknown Location";
    }
}