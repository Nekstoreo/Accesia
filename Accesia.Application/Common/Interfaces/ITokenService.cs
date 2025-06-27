namespace Accesia.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateEmailVerificationToken();
    string GeneratePasswordResetToken();
    string GenerateSecureToken(int length = 32);
} 