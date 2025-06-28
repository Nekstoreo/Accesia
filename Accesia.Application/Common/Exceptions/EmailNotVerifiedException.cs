namespace Accesia.Application.Common.Exceptions;

public class EmailNotVerifiedException : Exception
{
    public string Email { get; }

    public EmailNotVerifiedException(string email) 
        : base($"El email {email} no ha sido verificado. Por favor, verifica tu email antes de iniciar sesión.")
    {
        Email = email;
    }

    public EmailNotVerifiedException(string email, Exception innerException) 
        : base($"El email {email} no ha sido verificado. Por favor, verifica tu email antes de iniciar sesión.", innerException)
    {
        Email = email;
    }
} 