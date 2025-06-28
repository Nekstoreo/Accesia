using System;

namespace Accesia.Application.Common.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public string Email { get; }

        public UserNotFoundException(string message, string email)
            : base(message)
        {
            Email = email;
        }
    }
} 