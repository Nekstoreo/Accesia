using Accesia.Domain.Entities;

namespace Accesia.Application.Common.DTOs;

public class SecuritySearchCriteria
{
    public Guid? UserId { get; set; }
    public string? EventType { get; set; }
    public string? EventCategory { get; set; }
    public string? Severity { get; set; }
    public string? IpAddress { get; set; }
    public bool? IsSuccessful { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchText { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

public class SecurityTrend
{
    public string Period { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int HighSeverityEvents { get; set; }
    public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;
}

public enum TrendInterval
{
    Hourly,
    Daily,
    Weekly,
    Monthly
}

public class IntegrityVerificationReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalLogsChecked { get; set; }
    public int ValidLogs { get; set; }
    public int InvalidLogs { get; set; }
    public List<Guid> CorruptedLogIds { get; set; } = new();
    public bool HasErrors { get; set; }
    public double IntegrityPercentage => TotalLogsChecked > 0 ? (double)ValidLogs / TotalLogsChecked * 100 : 0;
}

public class ChainIntegrityReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalLogsInChain { get; set; }
    public bool IsValid { get; set; }
    public List<ChainBreak> BrokenLinks { get; set; } = new();
    public bool HasErrors { get; set; }
}

public class ChainBreak
{
    public Guid LogId { get; set; }
    public DateTime OccurredAt { get; set; }
    public ChainBreakType BreakType { get; set; }
    public string Description { get; set; } = string.Empty;
}

public enum ChainBreakType
{
    CorruptedLog,
    SuspiciousGap,
    MissingLog,
    TimestampAnomaly
} 