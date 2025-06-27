namespace Accesia.Application.Common.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email) 
        : base($"Ya existe una cuenta registrada con el email {email}")
    {
        Email = email;
    }

    public EmailAlreadyExistsException(string email, string message) : base(message)
    {
        Email = email;
    }
} 