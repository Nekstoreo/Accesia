namespace Accesia.Application.Common.Exceptions;

public class PasswordRecentlyUsedException : Exception
{
    public PasswordRecentlyUsedException() 
        : base("La contraseña proporcionada fue utilizada recientemente. Por favor, elija una contraseña diferente.")
    {
    }

    public PasswordRecentlyUsedException(string message) 
        : base(message)
    {
    }

    public PasswordRecentlyUsedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
} 