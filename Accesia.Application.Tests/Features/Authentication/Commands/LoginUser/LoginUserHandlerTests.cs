using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.LoginUser;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Collections.Generic; // Required for List

namespace Accesia.Application.Tests.Features.Authentication.Commands.LoginUser
{
    public class LoginUserHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IPasswordHashService> _mockPasswordHashService;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IDeviceInfoService> _mockDeviceInfoService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<ILogger<LoginUserHandler>> _mockLogger;
        private readonly LoginUserHandler _handler;

        public LoginUserHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockPasswordHashService = new Mock<IPasswordHashService>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockSessionService = new Mock<ISessionService>();
            _mockDeviceInfoService = new Mock<IDeviceInfoService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockLogger = new Mock<ILogger<LoginUserHandler>>();

            _handler = new LoginUserHandler(
                _mockContext.Object,
                _mockPasswordHashService.Object,
                _mockJwtTokenService.Object,
                _mockSessionService.Object,
                _mockDeviceInfoService.Object,
                _mockRateLimitService.Object,
                _mockLogger.Object
            );

            var mockUserDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<User>>();
            _mockContext.Setup(c => c.Users).Returns(mockUserDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(It.IsAny<string>(), "login", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Default to allow
        }

        private LoginUserCommand CreateLoginUserCommand(string email = "test@example.com", string password = "Password123!")
        {
            return new LoginUserCommand
            {
                Email = email,
                Password = password,
                RememberMe = false,
                DeviceName = "Test Device",
                IpAddress = "127.0.0.1",
                UserAgent = "TestAgent/1.0"
            };
        }

        private User CreateTestUser(
            string email = "test@example.com",
            string passwordHash = "hashed_password",
            bool isEmailVerified = true,
            UserStatus status = UserStatus.Active,
            DateTime? lockedUntil = null,
            List<UserRole>? userRoles = null)
        {
            var user = User.CreateNewUser(new Email(email), passwordHash, "Test", "User");
            user.IsEmailVerified = isEmailVerified;
            user.Status = status;
            user.LockedUntil = lockedUntil;
            if (isEmailVerified) user.EmailVerifiedAt = DateTime.UtcNow.AddDays(-1);

            // Simulate EF Core loading related entities if needed by the handler
            // For roles and permissions, we need to ensure UserRoles collection is populated
            // and that Role and Permission entities within are accessible.
            if (userRoles != null)
            {
                user.UserRoles = userRoles;
            }
            else // Default role if none provided, for basic permission checks
            {
                 var defaultRole = new Role { Name = "User", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission>() };
                 var defaultUserRole = new UserRole { UserId = user.Id, RoleId = defaultRole.Id, Role = defaultRole, IsActive = true };
                 user.UserRoles = new List<UserRole> { defaultUserRole };
            }

            return user;
        }

        private Session CreateTestSession(Guid userId)
        {
            return Session.CreateNewSession(
                new User(new Email("test@example.com"), "hash", "fn", "ln") { Id = userId }, // Simplified user for session creation
                new DeviceInfo { Browser = "TestBrowser", OperatingSystem = "TestOS", DeviceFingerprint = "fp1", UserAgent = "ua" },
                new LocationInfo { IpAddress = "127.0.0.1", City = "TestCity", Country = "TestCountry" },
                "Password"
            );
        }

        [Fact]
        public async Task Handle_ValidCredentialsAndVerifiedUser_ShouldReturnLoginResponse()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            var user = CreateTestUser(email: command.Email, isEmailVerified: true, status: UserStatus.Active);
            var session = CreateTestSession(user.Id);
            var accessToken = "test_access_token";
            var tokenExpiration = DateTime.UtcNow.AddHours(1);

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockPasswordHashService.Setup(s => s.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _mockDeviceInfoService.Setup(s => s.ExtractDeviceInfo(command.UserAgent))
                .Returns(new DeviceInfo { Browser = "Chrome", OperatingSystem = "Windows", UserAgent = command.UserAgent, DeviceFingerprint = "fingerprint123" });
            _mockDeviceInfoService.Setup(s => s.ExtractLocationInfo(command.IpAddress))
                .Returns(new LocationInfo { IpAddress = command.IpAddress, City = "Test City", Country = "Test Country" });
            _mockSessionService.Setup(s => s.CreateSessionAsync(user, It.IsAny<DeviceInfo>(), It.IsAny<LocationInfo>(), "Password", It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            _mockJwtTokenService.Setup(s => s.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns(accessToken);
            _mockJwtTokenService.Setup(s => s.GetTokenExpiration()).Returns(tokenExpiration);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accessToken, result.AccessToken);
            Assert.Equal(session.RefreshToken, result.RefreshToken);
            Assert.Equal(user.Email.Value, result.User.Email);
            Assert.Equal(session.SessionToken, result.Session.SessionId);

            _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // For user.OnSuccessfulLogin()
            // User.OnSuccessfulLogin is internal to the User entity, but its effects (LastLoginAt, FailedLoginAttempts) can be checked if needed
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldThrowUserNotFoundException()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Email, exception.Email);
             _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AccountLocked_ShouldThrowAccountLockedException()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            var lockedUntil = DateTime.UtcNow.AddHours(1);
            var user = CreateTestUser(email: command.Email, lockedUntil: lockedUntil, status: UserStatus.Blocked);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AccountLockedException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Email, exception.Email);
            Assert.Equal(lockedUntil, exception.LockedUntil);
             _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailNotVerified_ShouldThrowEmailNotVerifiedException()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            var user = CreateTestUser(email: command.Email, isEmailVerified: false);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EmailNotVerifiedException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Email, exception.Email);
             _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ShouldIncrementFailedAttemptsAndThrowInvalidCredentialsException()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            var user = CreateTestUser(email: command.Email); // Initially 0 failed attempts
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockPasswordHashService.Setup(s => s.VerifyPassword(command.Password, user.PasswordHash)).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Email, exception.Email);
            Assert.Equal(1, user.FailedLoginAttempts); // Check if User.IncrementFailedLoginAttempts was effectively called
             _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // For saving updated FailedLoginAttempts
        }


        [Fact]
        public async Task Handle_InvalidPassword_ShouldLockAccountAfterMaxAttempts()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            // User is created with 0 failed attempts. Max attempts is 5 (internal to User entity).
            // We'll simulate this by setting FailedLoginAttempts to 4 before the call.
            // The handler will call IncrementFailedLoginAttempts, making it 5, which should trigger lock.
            var user = CreateTestUser(email: command.Email);
            user.FailedLoginAttempts = 4; // One attempt away from locking (assuming max 5)

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockPasswordHashService.Setup(s => s.VerifyPassword(command.Password, user.PasswordHash)).Returns(false);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Equal(command.Email, exception.Email);
            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.True(user.IsAccountLocked()); // Check if User.LockAccount was effectively called
            Assert.Equal(UserStatus.Blocked, user.Status);
             _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_RateLimitExceeded_ShouldThrowRateLimitExceededException()
        {
            // Arrange
            var command = CreateLoginUserCommand();
            var cooldown = TimeSpan.FromSeconds(30);
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimitService.Setup(s => s.GetRemainingCooldownAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cooldown);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(cooldown, exception.RetryAfter);
            Assert.Equal("login", exception.Action);
            _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.IpAddress, "login", It.IsAny<CancellationToken>()), Times.Never); // Should not record if rate limited before user lookup
        }
    }
}
