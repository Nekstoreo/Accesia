using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.VerifyEmail;
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

namespace Accesia.Application.Tests.Features.Authentication.Commands.VerifyEmail
{
    public class VerifyEmailHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<VerifyEmailHandler>> _mockLogger;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly VerifyEmailHandler _handler;

        public VerifyEmailHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<VerifyEmailHandler>>();
            _mockRateLimitService = new Mock<IRateLimitService>();

            _handler = new VerifyEmailHandler(
                _mockContext.Object,
                _mockLogger.Object,
                _mockRateLimitService.Object
            );

            var mockUserDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<User>>();
            _mockContext.Setup(c => c.Users).Returns(mockUserDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Default to allow action for most tests
        }

        private VerifyEmailCommand CreateVerifyEmailCommand(string token = "valid_token", string email = "test@example.com", string ip = "127.0.0.1")
        {
            return new VerifyEmailCommand
            {
                Token = token,
                Email = email,
                ClientIpAddress = ip
            };
        }

        private User CreateTestUser(
            string email = "test@example.com",
            string? token = "valid_token",
            DateTime? tokenExpiresAt = null,
            bool isVerified = false,
            UserStatus status = UserStatus.PendingConfirmation)
        {
            var user = User.CreateNewUser(new Email(email), "hashed_password", "Test", "User");
            user.EmailVerificationToken = token;
            user.EmailVerificationTokenExpiresAt = tokenExpiresAt ?? DateTime.UtcNow.AddHours(1);
            user.IsEmailVerified = isVerified;
            user.Status = status;
            if (isVerified) user.EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5);
            return user;
        }

        [Fact]
        public async Task Handle_ValidTokenAndUserPending_ShouldVerifyEmailActivateUserAndClearToken()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var user = CreateTestUser(token: command.Token, status: UserStatus.PendingConfirmation);

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Email verificado correctamente", result.Message);
            Assert.True(user.IsEmailVerified);
            Assert.NotNull(user.EmailVerifiedAt);
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.Null(user.EmailVerificationToken);
            Assert.Null(user.EmailVerificationTokenExpiresAt);
            Assert.True(result.IsAccountActivated);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockRateLimitService.Verify(s => s.RecordActionAttemptAsync(command.ClientIpAddress, "email_verification", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidTokenAndUserEmailPendingVerification_ShouldVerifyEmailActivateUser()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var user = CreateTestUser(token: command.Token, status: UserStatus.EmailPendingVerification);

            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.True(user.IsEmailVerified);
            Assert.True(result.IsAccountActivated);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Handle_InvalidToken_ShouldThrowInvalidVerificationTokenException()
        {
            // Arrange
            var command = CreateVerifyEmailCommand(token: "invalid_token");
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Token, exception.Token);
            Assert.Equal(command.Email, exception.UserIdentifier);
        }

        [Fact]
        public async Task Handle_ExpiredToken_ShouldThrowExpiredVerificationTokenException()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var user = CreateTestUser(token: command.Token, tokenExpiresAt: DateTime.UtcNow.AddHours(-1));
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExpiredVerificationTokenException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Token, exception.Token);
            Assert.Equal(command.Email, exception.UserIdentifier);
        }

        [Fact]
        public async Task Handle_UserAlreadyVerified_ShouldThrowEmailAlreadyVerifiedException()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var user = CreateTestUser(token: command.Token, isVerified: true, status: UserStatus.Active);
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EmailAlreadyVerifiedException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.Token, exception.Token);
             Assert.Equal(command.Email, exception.Email); // Corrected: EmailAlreadyVerifiedException has Email property
        }

        [Fact]
        public async Task Handle_UserStatusNotPendingOrEmailPending_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var user = CreateTestUser(token: command.Token, status: UserStatus.Blocked); // Not PendingConfirmation or EmailPendingVerification
            _mockContext.Setup(c => c.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RateLimitExceeded_ShouldThrowRateLimitExceededException()
        {
            // Arrange
            var command = CreateVerifyEmailCommand();
            var cooldown = TimeSpan.FromMinutes(10);
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.ClientIpAddress, "email_verification", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimitService.Setup(s => s.GetRemainingCooldownAsync(command.ClientIpAddress, "email_verification", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cooldown);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(cooldown, exception.RetryAfter);
        }
    }
}
