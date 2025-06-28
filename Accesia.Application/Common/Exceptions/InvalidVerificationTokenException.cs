namespace Accesia.Application.Common.Exceptions;

public class InvalidVerificationTokenException : Exception
{
    public InvalidVerificationTokenException(string message, string? token, string? email = null)
        : base(message)
    {
        Token = token;
        Email = email;
    }

    public string? Token { get; }
    public string? Email { get; }
}