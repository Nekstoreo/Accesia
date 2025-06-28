namespace Accesia.Application.Features.Authentication.DTOs;

public record RefreshTokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string TokenType { get; init; } = "Bearer";
    public required int ExpiresIn { get; init; }
    public required DateTime ExpiresAt { get; init; }
} 