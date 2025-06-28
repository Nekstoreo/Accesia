namespace Accesia.Application.Common.Exceptions;

public class InvalidCredentialsException : Exception
{
    public string Email { get; }
    public int RemainingAttempts { get; }

    public InvalidCredentialsException(string email, int remainingAttempts) 
        : base($"Credenciales inválidas para el email: {email}. Intentos restantes: {remainingAttempts}")
    {
        Email = email;
        RemainingAttempts = remainingAttempts;
    }

    public InvalidCredentialsException(string email, int remainingAttempts, Exception innerException) 
        : base($"Credenciales inválidas para el email: {email}. Intentos restantes: {remainingAttempts}", innerException)
    {
        Email = email;
        RemainingAttempts = remainingAttempts;
    }
} 