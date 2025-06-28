namespace Accesia.Application.Common.Exceptions;

public class CurrentPasswordIncorrectException : Exception
{
    public CurrentPasswordIncorrectException()
        : base("La contraseña actual proporcionada es incorrecta.")
    {
    }

    public CurrentPasswordIncorrectException(string message)
        : base(message)
    {
    }

    public CurrentPasswordIncorrectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}