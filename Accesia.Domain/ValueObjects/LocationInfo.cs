namespace Accesia.Domain.ValueObjects;

public class LocationInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? ISP { get; set; }
    public bool IsVPN { get; set; }

    // Constructor sin parámetros para EF Core
    public LocationInfo() { }

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

    public static LocationInfo CreateFromIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address no puede estar vacía", nameof(ipAddress));

        // En una implementación real, aquí consultarías un servicio de geolocalización
        // como MaxMind GeoIP2, IP2Location, etc.
        return new LocationInfo(
            ipAddress: ipAddress,
            country: "Unknown",
            city: "Unknown",
            region: "Unknown",
            isp: "Unknown",
            isVPN: false
        );
    }

    public static LocationInfo CreateLocalhost()
    {
        return new LocationInfo(
            ipAddress: "127.0.0.1",
            country: "Local",
            city: "Local",
            region: "Local",
            isp: "Local",
            isVPN: false
        );
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
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
        if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
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
