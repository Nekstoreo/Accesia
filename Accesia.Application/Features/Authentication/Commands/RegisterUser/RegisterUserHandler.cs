using MediatR;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;

namespace Accesia.Application.Features.Authentication.Commands.RegisterUser;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RegisterUserHandler> _logger;
    // TODO: Agregar servicios que necesitarás:
    // private readonly IPasswordHashService _passwordHashService;
    // private readonly IEmailService _emailService;
    // private readonly IRateLimitService _rateLimitService;
    /*
        Lógica que necesitas implementar en el Handle:
        - Verificar rate limiting por IP
        - Verificar que el email no esté registrado
        - Crear value objects (Email, Password)
        - Hashear la contraseña con BCrypt
        - Generar token de verificación de email
        - Crear entidad User
        - Guardar en base de datos
        - Enviar email de verificación
        - Retornar respuesta exitosa
    */

    public RegisterUserHandler(
        IApplicationDbContext context,
        ILogger<RegisterUserHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implementar lógica aquí
        throw new NotImplementedException();
    }
}