namespace Accesia.Application.Features.Authentication.DTOs;

public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string TokenType { get; init; } = "Bearer";
    public required int ExpiresIn { get; init; }
    public required UserInfoDto User { get; init; }
    public required SessionInfoDto Session { get; init; }
}

public record UserInfoDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Status { get; init; }
    public required bool IsEmailVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required IEnumerable<string> Roles { get; init; }
    public required IEnumerable<string> Permissions { get; init; }
}

public record SessionInfoDto
{
    public required string SessionId { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string DeviceInfo { get; init; }
    public required string LocationInfo { get; init; }
    public required bool IsKnownDevice { get; init; }
} 