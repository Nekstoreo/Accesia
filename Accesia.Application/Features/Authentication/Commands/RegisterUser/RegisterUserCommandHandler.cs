using MediatR;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Entities;
using Accesia.Application.Common.Exceptions;

namespace Accesia.Application.Features.Authentication.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        ILogger<RegisterUserCommandHandler> logger,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando proceso de registro para email {Email}", request.Email);

        // Verificar Unicidad de Email
        await ValidateEmailNotExistsAsync(request.Email, cancellationToken);

        // Crear Value Objects
        var email = new Email(request.Email);
        var password = new Password(request.Password);

        // Hashear la contraseña con BCrypt
        var passwordHash = _passwordHasher.HashPassword(password.Value);

        // Generar token de verificación de email
        var verificationToken = _tokenService.GenerateEmailVerificationToken();
        var tokenExpiration = DateTime.UtcNow.AddHours(24); // Token válido por 24 horas

        // Crear entidad User con estado pendiente de verificación
        var user = User.CreateNewUser(email, passwordHash, request.FirstName, request.LastName);

        // Configurar propiedades adicionales
        user.PhoneNumber = request.PhoneNumber;

        // Establecer token de verificación
        user.SetEmailVerificationToken(verificationToken, tokenExpiration);

        try
        {
            // 7. Guardar en base de datos
            await _userRepository.CreateUserAsync(user);

            _logger.LogInformation("Usuario creado exitosamente con ID {UserId} para email {Email}",
                user.Id, request.Email);

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

            // 8. Retornar respuesta exitosa (sin información sensible)
            return new RegisterUserResponse
            {
                Success = true,
                Message = "Usuario registrado exitosamente. Se ha enviado un email de verificación.",
                UserId = user.Id,
                Email = user.Email.Value,
                RequiresEmailVerification = true,
                ValidationErrors = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar usuario en base de datos para email {Email}", request.Email);
            throw;
        }
    }
    private async Task ValidateEmailNotExistsAsync(string email, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetUserByEmailAsync(email);

        if (existingUser != null)
        {
            _logger.LogWarning("Intento de registro con email ya existente: {Email}", email);
            throw new EmailAlreadyExistsException(email);
        }
    }
}