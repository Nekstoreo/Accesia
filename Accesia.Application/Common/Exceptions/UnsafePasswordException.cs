namespace Accesia.Application.Common.Exceptions;

public class UnsafePasswordException : Exception
{
    public IEnumerable<string> SecuritySuggestions { get; }

    public UnsafePasswordException() 
        : base("La contraseña no cumple con los estándares de seguridad requeridos.")
    {
        SecuritySuggestions = Enumerable.Empty<string>();
    }

    public UnsafePasswordException(string message) 
        : base(message)
    {
        SecuritySuggestions = Enumerable.Empty<string>();
    }

    public UnsafePasswordException(string message, IEnumerable<string> securitySuggestions) 
        : base(message)
    {
        SecuritySuggestions = securitySuggestions ?? Enumerable.Empty<string>();
    }

    public UnsafePasswordException(string message, Exception innerException) 
        : base(message, innerException)
    {
        SecuritySuggestions = Enumerable.Empty<string>();
    }
} 