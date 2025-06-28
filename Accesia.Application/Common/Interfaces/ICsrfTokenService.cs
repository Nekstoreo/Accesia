namespace Accesia.Application.Common.Interfaces;

public interface ICsrfTokenService
{
    string GenerateToken(Guid userId);
    bool ValidateToken(string token, Guid userId);
    string? ExtractTokenFromHeaders(IDictionary<string, string> headers);
}