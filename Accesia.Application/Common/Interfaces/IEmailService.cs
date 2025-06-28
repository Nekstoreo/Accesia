namespace Accesia.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string verificationToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default);

    Task SendPasswordChangeNotificationAsync(string email, string firstName, DateTime changedAt, string deviceInfo,
        CancellationToken cancellationToken = default);

    Task SendEmailChangeVerificationAsync(string newEmail, string firstName, string verificationToken,
        CancellationToken cancellationToken = default);

    Task SendAccountDeletionConfirmationEmailAsync(string email, string fullName, string deletionToken,
        DateTime tokenExpiration, CancellationToken cancellationToken = default);

    Task SendAccountMarkedForDeletionEmailAsync(string email, string fullName, DateTime permanentDeletionDate,
        CancellationToken cancellationToken = default);

    Task SendAccountDeletionCancelledEmailAsync(string email, string fullName,
        CancellationToken cancellationToken = default);
}