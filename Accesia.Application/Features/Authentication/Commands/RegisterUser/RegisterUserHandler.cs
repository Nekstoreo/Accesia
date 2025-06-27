using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Entities;

namespace Accesia.Application.Features.Authentication.Commands.RegisterUser;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RegisterUserHandler> _logger;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IEmailService _emailService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ITokenService _tokenService;

    public RegisterUserHandler(
        IApplicationDbContext context,
        ILogger<RegisterUserHandler> logger,
        IPasswordHashService passwordHashService,
        IEmailService emailService,
        IRateLimitService rateLimitService,
        ITokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _passwordHashService = passwordHashService;
        _emailService = emailService;
        _rateLimitService = rateLimitService;
        _tokenService = tokenService;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando proceso de registro para email {Email}", request.Email);

        // 1. Verificar rate limiting por IP
        await CheckRateLimitAsync(request.ClientIpAddress);

        // 2. Validar que el email no esté registrado
        await ValidateEmailNotExistsAsync(request.Email, cancellationToken);

        // 3. Crear value objects y validar contraseña
        var email = new Email(request.Email);
        var password = new Password(request.Password);

        // 4. Hashear la contraseña con BCrypt
        var passwordHash = _passwordHashService.HashPassword(password.Value);

        // 5. Generar token de verificación de email
        var verificationToken = _tokenService.GenerateEmailVerificationToken();
        var tokenExpiration = DateTime.UtcNow.AddHours(24); // Token válido por 24 horas

        // 6. Crear entidad User con estado pendiente de verificación
        var user = User.CreateNewUser(email, passwordHash, request.FirstName, request.LastName);
        
        // Configurar propiedades adicionales
        user.PhoneNumber = request.PhoneNumber;
        user.PreferredLanguage = request.PreferredLanguage;
        user.TimeZone = request.TimeZone;

        // Establecer token de verificación
        user.SetEmailVerificationToken(verificationToken, tokenExpiration);

        try
        {
            // 7. Guardar en base de datos
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuario creado exitosamente con ID {UserId} para email {Email}", 
                user.Id, request.Email);

            // 8. Registrar el intento en rate limiting
            await _rateLimitService.RecordActionAttemptAsync(
                request.ClientIpAddress ?? "unknown", 
                "user_registration");

            // 9. Enviar email de verificación de forma asíncrona
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailVerificationAsync(
                        user.Email.Value, 
                        verificationToken, 
                        CancellationToken.None);
                    
                    _logger.LogInformation("Email de verificación enviado a {Email}", request.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de verificación a {Email}", request.Email);
                }
            }, CancellationToken.None);

            // 10. Retornar respuesta exitosa (sin información sensible)
            return new RegisterUserResponse
            {
                Success = true,
                Message = "Usuario registrado exitosamente. Se ha enviado un email de verificación.",
                UserId = user.Id,
                Email = user.Email.Value,
                RequiresEmailVerification = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar usuario en base de datos para email {Email}", request.Email);
            throw;
        }
    }

    private async Task CheckRateLimitAsync(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return; // Si no tenemos IP, continuamos (en desarrollo puede pasar)

        var canRegister = await _rateLimitService.CanPerformActionAsync(ipAddress, "user_registration");
        
        if (!canRegister)
        {
            var cooldown = await _rateLimitService.GetRemainingCooldownAsync(ipAddress, "user_registration");
            _logger.LogWarning("Intento de registro bloqueado por rate limit desde IP {IpAddress}", ipAddress);
            throw new RateLimitExceededException(cooldown);
        }
    }

    private async Task ValidateEmailNotExistsAsync(string email, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("Intento de registro con email ya existente: {Email}", email);
            throw new EmailAlreadyExistsException(email);
        }
    }
}