using FluentValidation;
using Accesia.Application.Features.Authentication.DTOs;

namespace Accesia.Application.Features.Authentication.Validators
{
    public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token de verificación es obligatorio")
                .Length(32, 128).WithMessage("El token tiene un formato inválido")
                .Must(BeValidToken).WithMessage("El token tiene un formato inválido");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("El formato del email no es válido");
        }

        private bool BeValidToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

        try
        {
            // Verificar que el token tenga formato base64 URL-safe válido
            // Primero restaurar los caracteres base64 estándar
            var base64Token = token.Replace('-', '+').Replace('_', '/');
            
            // Agregar padding si es necesario
            switch (base64Token.Length % 4)
            {
                case 2: base64Token += "=="; break;
                case 3: base64Token += "="; break;
            }

            // Intentar decodificar el token
            Convert.FromBase64String(base64Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
    }
}
