namespace Accesia.Application.Common.Exceptions;

public class EmailAlreadyVerifiedException : Exception
{
    public EmailAlreadyVerifiedException(string message, string? token, string? email = null)
        : base(message)
    {
        Token = token;
        Email = email;
    }

    public string? Token { get; }
    public string? Email { get; }
}