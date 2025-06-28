namespace Accesia.Application.Common.Exceptions;

public class InvalidPasswordResetTokenException : Exception
{
    public InvalidPasswordResetTokenException()
        : base("El token de restablecimiento de contraseña es inválido o ha expirado.")
    {
    }

    public InvalidPasswordResetTokenException(string message)
        : base(message)
    {
    }

    public InvalidPasswordResetTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}