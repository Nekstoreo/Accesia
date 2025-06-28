using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;

namespace Accesia.Application.Features.Authentication.Commands.LoginUser;

public class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        IApplicationDbContext context,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IDeviceInfoService deviceInfoService,
        IRateLimitService rateLimitService,
        ILogger<LoginUserHandler> logger)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _deviceInfoService = deviceInfoService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // Verificar rate limiting
        if (!await _rateLimitService.CanPerformActionAsync(request.IpAddress, "login", cancellationToken))
        {
            var cooldown = await _rateLimitService.GetRemainingCooldownAsync(request.IpAddress, "login", cancellationToken);
            throw new RateLimitExceededException("login", cooldown);
        }
        
        await _rateLimitService.RecordActionAttemptAsync(request.IpAddress, "login", cancellationToken);

        // Buscar usuario por email
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Intento de login con email inexistente: {Email} desde IP: {IpAddress}", 
                request.Email, request.IpAddress);
            throw new UserNotFoundException("Usuario no encontrado", request.Email);
        }

        // Verificar que el usuario puede iniciar sesión
        if (!user.CanLogin())
        {
            _logger.LogWarning("Intento de login denegado para {Email}. Estado: {Status}, Bloqueada: {IsLocked}", 
                request.Email, user.Status, user.IsAccountLocked());
                
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Intento de login con email no verificado: {Email}", request.Email);
                throw new EmailNotVerifiedException(request.Email);
            }
                
            if (user.RequiresReactivation())
                throw new AccountInactiveException(request.Email);
            
            if (user.IsAccountLocked())
                throw new AccountLockedException(request.Email, user.LockedUntil!.Value);
                
            if (user.Status == UserStatus.Blocked)
                throw new AccountBlockedException(request.Email, user.LockedUntil);
                
            if (user.Status == UserStatus.MarkedForDeletion)
                throw new AccountMarkedForDeletionException(request.Email);
            
            throw new CannotPerformActionException(request.Email, "login", "Estado de cuenta no permite iniciar sesión");
        }

        // Verificar contraseña
        if (!_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.IncrementFailedLoginAttempts();
            await _context.SaveChangesAsync(cancellationToken);

            var remainingAttempts = Math.Max(0, 5 - user.FailedLoginAttempts); // 5 es configurable
            
            _logger.LogWarning("Credenciales inválidas para {Email}. Intentos fallidos: {FailedAttempts}", 
                request.Email, user.FailedLoginAttempts);
                
            throw new InvalidCredentialsException(request.Email, remainingAttempts);
        }

        // Login exitoso
        user.OnSuccessfulLogin();

        // Extraer información del dispositivo y ubicación
        var deviceInfo = _deviceInfoService.ExtractDeviceInfo(request.UserAgent);
        var locationInfo = _deviceInfoService.ExtractLocationInfo(request.IpAddress);

        // Nota: DeviceName se manejará a nivel de sesión

        // Crear sesión
        var session = await _sessionService.CreateSessionAsync(
            user, deviceInfo, locationInfo, "Password", cancellationToken);
            
        // Asignar nombre de dispositivo si se proporcionó
        if (!string.IsNullOrEmpty(request.DeviceName))
        {
            session.DeviceName = request.DeviceName;
        }

        // Generar tokens
        var roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name);
        var permissions = user.GetEffectivePermissions().Select(p => p.Name);
        
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var tokenExpiration = _jwtTokenService.GetTokenExpiration();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login exitoso para usuario {Email} desde IP {IpAddress}", 
            request.Email, request.IpAddress);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = session.RefreshToken ?? string.Empty,
            TokenType = "Bearer",
            ExpiresIn = (int)(tokenExpiration - DateTime.UtcNow).TotalSeconds,
            User = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                LastLoginAt = user.LastLoginAt,
                Roles = roles,
                Permissions = permissions
            },
            Session = new SessionInfoDto
            {
                SessionId = session.SessionToken,
                ExpiresAt = session.ExpiresAt,
                DeviceInfo = $"{session.DeviceInfo.Browser} en {session.DeviceInfo.OperatingSystem}",
                LocationInfo = $"{session.LocationInfo.IpAddress}",
                IsKnownDevice = session.IsKnownDevice
            }
        };
    }
} 