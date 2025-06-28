namespace Accesia.Application.Common.Exceptions;

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(TimeSpan retryAfter)
        : base($"Se ha excedido el límite de intentos. Intenta nuevamente en {retryAfter.TotalMinutes:F0} minutos.")
    {
        RetryAfter = retryAfter;
    }

    public RateLimitExceededException(string message, TimeSpan retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan RetryAfter { get; }
}