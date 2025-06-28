using Accesia.Domain.ValueObjects;

namespace Accesia.Application.Common.Interfaces;

public interface IDeviceInfoService
{
    DeviceInfo ExtractDeviceInfo(string userAgent, string? additionalInfo = null);
    LocationInfo ExtractLocationInfo(string ipAddress, string? forwardedFor = null);
    bool IsKnownDevice(Guid userId, DeviceInfo deviceInfo);
    Task<bool> IsKnownDeviceAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default);
} 