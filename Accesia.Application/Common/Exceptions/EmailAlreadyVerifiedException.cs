using System;

namespace Accesia.Application.Common.Exceptions
{
    public class EmailAlreadyVerifiedException : Exception
    {
        public string Token { get; }
        public string? Email { get; }

        public EmailAlreadyVerifiedException(string message, string token, string? email = null)
            : base(message)
        {
            Token = token;
            Email = email;
        }
    }
}
