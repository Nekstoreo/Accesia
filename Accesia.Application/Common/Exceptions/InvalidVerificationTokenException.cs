using System;

namespace Accesia.Application.Common.Exceptions
{
    public class InvalidVerificationTokenException : Exception
    {
        public string Token { get; }
        public string? Email { get; }

        public InvalidVerificationTokenException(string message, string token, string? email = null)
            : base(message)
        {
            Token = token;
            Email = email;
        }
    }
}
