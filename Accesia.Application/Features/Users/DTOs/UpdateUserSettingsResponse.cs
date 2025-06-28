namespace Accesia.Application.Features.Users.DTOs;

public class UpdateUserSettingsResponse
{
    public Guid UserId { get; set; }
    public IEnumerable<string> UpdatedSections { get; set; } = new List<string>();
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}