using System;

namespace Accesia.Application.Common.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public string? Email { get; }
        public Guid? UserId { get; }

        public UserNotFoundException(string message, string email)
            : base(message)
        {
            Email = email;
        }

        public UserNotFoundException(Guid userId)
            : base($"Usuario con ID {userId} no encontrado")
        {
            UserId = userId;
        }
    }
} 