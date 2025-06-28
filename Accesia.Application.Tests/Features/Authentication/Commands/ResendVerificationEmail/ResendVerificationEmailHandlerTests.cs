using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.ResendVerificationEmail;
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

namespace Accesia.Application.Tests.Features.Authentication.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Pruebas unitarias para <see cref="ResendVerificationEmailHandler"/>.
    /// </summary>
    public class ResendVerificationEmailHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<ILogger<ResendVerificationEmailHandler>> _mockLogger;
        private readonly ResendVerificationEmailHandler _handler;

        public ResendVerificationEmailHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockEmailService = new Mock<IEmailService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockLogger = new Mock<ILogger<ResendVerificationEmailHandler>>();

            _handler = new ResendVerificationEmailHandler(
                _mockContext.Object,
                _mockEmailService.Object,
                _mockTokenService.Object,
                _mockRateLimitService.Object,
                _mockLogger.Object
            );

            var mockUserDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<User>>();
            _mockContext.Setup(c => c.Users).Returns(mockUserDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Default to allow
        }

        private ResendVerificationEmailCommand CreateCommand(string email = "test@example.com", string ip = "127.0.0.1")
        {
            return new ResendVerificationEmailCommand { Email = email, ClientIpAddress = ip };
        }

        private User CreateTestUser(string email, bool isVerified = false, UserStatus status = UserStatus.PendingConfirmation, string? currentToken = null)
        {
            var user = User.CreateNewUser(new Email(email), "hashed_password", "Test", "User");
            user.IsEmailVerified = isVerified;
            user.Status = status;
            if (isVerified) user.EmailVerifiedAt = DateTime.UtcNow.AddDays(-1);
            if (!isVerified && currentToken == null) // Give a default old token if not verified and no specific token is given
            {
                user.SetEmailVerificationToken("old_token", DateTime.UtcNow.AddHours(-24));
            }
            else if (currentToken != null)
            {
                 user.SetEmailVerificationToken(currentToken, DateTime.UtcNow.AddHours(24));
            }
            return user;
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldThrowUserNotFoundException()
        {
            // Arrange
            var command = CreateCommand("nonexistent@example.com");
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailAlreadyVerified_ShouldThrowEmailAlreadyVerifiedException()
        {
            // Arrange
            var email = "verified@example.com";
            var command = CreateCommand(email);
            var user = CreateTestUser(email, isVerified: true, status: UserStatus.Active);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<EmailAlreadyVerifiedException>(() => _handler.Handle(command, CancellationToken.None));
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotPendingAndNotActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var email = "blocked@example.com";
            var command = CreateCommand(email);
            var user = CreateTestUser(email, isVerified: false, status: UserStatus.Blocked);
             _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Handle_ValidRequestForUnverifiedUser_ShouldGenerateNewTokenSendEmailAndUpdateUser()
        {
            // Arrange
            var email = "unverified@example.com";
            var command = CreateCommand(email);
            var user = CreateTestUser(email, isVerified: false, status: UserStatus.PendingConfirmation);
            var newVerificationToken = "new_test_token";

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken()).Returns(newVerificationToken);
            _mockEmailService.Setup(s => s.SendEmailVerificationAsync(email, newVerificationToken, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Nuevo email de verificación enviado.", result.Message);
            Assert.Equal(newVerificationToken, user.EmailVerificationToken);
            Assert.NotNull(user.EmailVerificationTokenExpiresAt);
            Assert.True(user.EmailVerificationTokenExpiresAt > DateTime.UtcNow);

            _mockTokenService.Verify(s => s.GenerateEmailVerificationToken(), Times.Once);
            _mockEmailService.Verify(s => s.SendEmailVerificationAsync(email, newVerificationToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequestForActiveUserWithUnverifiedEmail_ShouldSendEmail()
        {
            // Arrange
            var email = "active_unverified@example.com";
            var command = CreateCommand(email);
            // User is active (e.g. admin created, or some other flow) but email not yet verified
            var user = CreateTestUser(email, isVerified: false, status: UserStatus.Active);
            var newVerificationToken = "new_active_token";

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken()).Returns(newVerificationToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            _mockEmailService.Verify(s => s.SendEmailVerificationAsync(email, newVerificationToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(newVerificationToken, user.EmailVerificationToken);
        }


        [Fact]
        public async Task Handle_RateLimitExceeded_ShouldThrowRateLimitExceededException()
        {
            // Arrange
            var command = CreateCommand("ratelimited@example.com");
            var cooldown = TimeSpan.FromMinutes(5);
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimitService.Setup(s => s.GetRemainingCooldownAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cooldown);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(cooldown, exception.RetryAfter);
             _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmailServiceFails_ShouldLogErrorButStillUpdateTokenAndSave()
        {
            // Arrange
            var email = "emailfail@example.com";
            var command = CreateCommand(email);
            var user = CreateTestUser(email, isVerified: false, status: UserStatus.PendingConfirmation);
            var newVerificationToken = "token_email_fail";

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockTokenService.Setup(s => s.GenerateEmailVerificationToken()).Returns(newVerificationToken);
            _mockEmailService.Setup(s => s.SendEmailVerificationAsync(email, newVerificationToken, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Email service down"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success); // The operation to update token should still succeed
            Assert.Equal(newVerificationToken, user.EmailVerificationToken); // Token updated in user entity
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // Changes saved

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error al enviar email de verificación")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.ClientIpAddress, "resend_verification_email", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
