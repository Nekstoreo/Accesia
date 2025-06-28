namespace Accesia.Application.Common.Exceptions;

public class ExpiredVerificationTokenException : Exception
{
    public ExpiredVerificationTokenException(string message, string? token, string? email = null)
        : base(message)
    {
        Token = token;
        Email = email;
    }

    public string? Token { get; }
    public string? Email { get; }
}