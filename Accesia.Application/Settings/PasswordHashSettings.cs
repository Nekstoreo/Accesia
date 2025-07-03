namespace Accesia.Application.Settings;

public record PasswordHashSettings
{
    public const string SectionName = "PasswordHashSettings";
    public int WorkFactor { get; set; }
    public int RehashWorkFactorThreshold { get; set; }
}