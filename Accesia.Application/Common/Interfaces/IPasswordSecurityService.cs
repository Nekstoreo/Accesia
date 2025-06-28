namespace Accesia.Application.Common.Interfaces;

public interface IPasswordSecurityService
{
    bool IsPasswordSafe(string password);
    Task<bool> IsPasswordSafeAsync(string password, CancellationToken cancellationToken = default);
    IEnumerable<string> GetPasswordSecuritySuggestions(string password);
} 