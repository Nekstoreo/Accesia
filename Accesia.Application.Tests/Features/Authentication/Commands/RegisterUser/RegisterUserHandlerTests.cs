using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.RegisterUser;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Entities;
using Accesia.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking; // Required for CollectionEntry

namespace Accesia.Application.Tests.Features.Authentication.Commands.RegisterUser
{
    /// <summary>
    /// Pruebas unitarias para <see cref="RegisterUserHandler"/>.
    /// Estas pruebas se enfocan en aislar la lógica del handler, utilizando mocks para
    /// todas sus dependencias externas como IApplicationDbContext, IEmailService, IPasswordHashService, etc.
    /// El objetivo es verificar que el handler interactúa correctamente con estas dependencias
    /// y produce el resultado esperado o lanza las excepciones adecuadas según el caso de prueba.
    /// </summary>
    public class RegisterUserHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<RegisterUserHandler>> _mockLogger;
        private readonly Mock<IPasswordHashService> _mockPasswordHashService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly RegisterUserHandler _handler;

        public RegisterUserHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<RegisterUserHandler>>();
            _mockPasswordHashService = new Mock<IPasswordHashService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockTokenService = new Mock<ITokenService>();

            _handler = new RegisterUserHandler(
                _mockContext.Object,
                _mockLogger.Object,
                _mockPasswordHashService.Object,
                _mockEmailService.Object,
                _mockRateLimitService.Object,
                _mockTokenService.Object
            );

            // Setup DbSet mock for Users
            var mockUserDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<User>>();
             _mockContext.Setup(c => c.Users).Returns(mockUserDbSet.Object);
             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1); // Simulate successful save

            // Setup for CollectionEntry - this is a bit more involved if you need to mock adding to collections
            // For basic Add, the DbSet mock is often enough. If User.CreateNewUser or other logic
            // manipulates collections that EF Core tracks, more setup might be needed.
            // For now, we assume User.CreateNewUser returns a valid User object and EF Core's Add handles it.
             _mockContext.Setup(c => c.Users.Add(It.IsAny<User>()))
                        .Callback<User>(user => { /* Optionally inspect the user object */ });
        }

        private RegisterUserCommand CreateValidRegisterUserCommand()
        {
            return new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FirstName = "Test",
                LastName = "User",
                ClientIpAddress = "127.0.0.1"
            };
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateUserAndSendVerificationEmail()
        {
            // Arrange
            var command = CreateValidRegisterUserCommand();
            var expectedVerificationToken = "test_token";
            User? capturedUser = null;

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null); // Email does not exist
            _mockPasswordHashService.Setup(s => s.HashPassword(command.Password))
                .Returns("hashed_password");
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken())
                .Returns(expectedVerificationToken);
            _mockEmailService.Setup(s => s.SendEmailVerificationAsync(command.Email, expectedVerificationToken, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
             _mockContext.Setup(c => c.Users.Add(It.IsAny<User>()))
                        .Callback<User>(u => capturedUser = u);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Usuario registrado exitosamente. Se ha enviado un email de verificación.", result.Message);
            Assert.Equal(command.Email, result.Email);
            Assert.True(result.RequiresEmailVerification);
            Assert.NotEqual(Guid.Empty, result.UserId); // UserId should be generated

            _mockContext.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Once);
            Assert.NotNull(capturedUser);
            Assert.Equal(command.Email, capturedUser.Email.Value);
            Assert.Equal("hashed_password", capturedUser.PasswordHash);
            Assert.Equal(UserStatus.PendingConfirmation, capturedUser.Status);
            Assert.Equal(expectedVerificationToken, capturedUser.EmailVerificationToken);
            Assert.NotNull(capturedUser.EmailVerificationTokenExpiresAt);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockPasswordHashService.Verify(s => s.HashPassword(command.Password), Times.Once);
            _mockTokenService.Verify(s => s.GenerateEmailVerificationToken(), Times.Once);
            _mockEmailService.Verify(s => s.SendEmailVerificationAsync(command.Email, expectedVerificationToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailAlreadyExists_ShouldThrowEmailAlreadyExistsException()
        {
            // Arrange
            var command = CreateValidRegisterUserCommand();
            var existingUser = User.CreateNewUser(new Email(command.Email), "somehash", "Existing", "User");

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Email, exception.Email);

            _mockEmailService.Verify(s => s.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RateLimitExceeded_ShouldThrowRateLimitExceededException()
        {
            // Arrange
            var command = CreateValidRegisterUserCommand();
            var cooldown = TimeSpan.FromMinutes(5);

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimitService.Setup(s => s.GetRemainingCooldownAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cooldown);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(cooldown, exception.RetryAfter);

            _mockContext.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmailServiceFails_ShouldStillCreateUserAndLogEmailError()
        {
            // Arrange
            var command = CreateValidRegisterUserCommand();
            var expectedVerificationToken = "test_token";
            User? capturedUser = null;

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockPasswordHashService.Setup(s => s.HashPassword(command.Password))
                .Returns("hashed_password");
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken())
                .Returns(expectedVerificationToken);
            _mockEmailService.Setup(s => s.SendEmailVerificationAsync(command.Email, expectedVerificationToken, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Email service failed"));
            _mockContext.Setup(c => c.Users.Add(It.IsAny<User>()))
                        .Callback<User>(u => capturedUser = u);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success); // Registration itself is successful
            _mockContext.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Once);
             Assert.NotNull(capturedUser);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify logger was called for the email sending error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error al enviar email de verificación")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DatabaseSaveFails_ShouldThrowExceptionAndNotSendEmail()
        {
            // Arrange
            var command = CreateValidRegisterUserCommand();
            var dbException = new DbUpdateException("DB save failed");

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "user_registration", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockPasswordHashService.Setup(s => s.HashPassword(command.Password))
                .Returns("hashed_password");
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken())
                .Returns("test_token");
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(dbException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(dbException.Message, exception.Message);

            _mockEmailService.Verify(s => s.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
