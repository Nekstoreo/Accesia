namespace Accesia.Application.Common.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email) : base($"El email {email} ya está registrado")
    {
        Email = email;
    }

    public EmailAlreadyExistsException(string email, string message) : base(message)
    {
        Email = email;
    }
}