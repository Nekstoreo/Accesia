using Accesia.Domain.ValueObjects;
using Accesia.Application.Common.Interfaces;

namespace Accesia.Application.Common.DTOs;

public class AlertContext
{
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public SecurityThreatLevel ThreatLevel { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public LocationInfo? LocationInfo { get; set; }
    public DeviceInfo DeviceInfo { get; set; } = null!;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
} 