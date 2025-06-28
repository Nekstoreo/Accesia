using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.Logout;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;

namespace Accesia.Application.Tests.Features.Authentication.Commands.Logout
{
    public class LogoutHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<ILogger<LogoutHandler>> _mockLogger;
        private readonly LogoutHandler _handler;

        public LogoutHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockSessionService = new Mock<ISessionService>();
            _mockLogger = new Mock<ILogger<LogoutHandler>>();

            _handler = new LogoutHandler(
                _mockContext.Object,
                _mockSessionService.Object,
                _mockLogger.Object
            );

            var mockSessionDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Session>>();
            _mockContext.Setup(c => c.Sessions).Returns(mockSessionDbSet.Object);
        }

        private LogoutCommand CreateLogoutCommand(string sessionToken = "valid_session_token")
        {
            return new LogoutCommand
            {
                SessionToken = sessionToken,
                IpAddress = "127.0.0.1",
                UserAgent = "TestAgent/1.0"
            };
        }

        private User CreateStubUser(string email = "test@example.com")
        {
            // User.cs has a private constructor and a factory method.
            // For stubbing, we might need to new it up directly if the factory has too many dependencies
            // or ensure all constructor parameters are provided.
            // Let's assume the factory method is usable or we can mock its creation.
            // For simplicity here, we'll new it up with minimal required fields for the test.
            var user = User.CreateNewUser(new Email(email), "passwordHash", "FirstName", "LastName");
            user.Id = Guid.NewGuid(); // Ensure Id is set for relationships
            return user;
        }


        private Session CreateTestSession(string token, User user, SessionStatus status = SessionStatus.Active)
        {
            var session = Session.CreateNewSession(
                user,
                new DeviceInfo { Browser = "Test", OperatingSystem = "TestOS", UserAgent = "TestAgent", DeviceFingerprint = "fp" },
                new LocationInfo { IpAddress = "127.0.0.1", City = "Test", Country = "Test" },
                "Password"
            );
            session.SessionToken = token; // Override generated token
            session.Status = status;
            session.User = user; // Ensure User navigation property is set
            return session;
        }

        [Fact]
        public async Task Handle_ValidActiveSessionToken_ShouldRevokeSessionAndReturnSuccess()
        {
            // Arrange
            var command = CreateLogoutCommand();
            var user = CreateStubUser();
            var session = CreateTestSession(command.SessionToken, user, SessionStatus.Active);

            _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(It.IsAny<Expression<Func<Session, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            _mockSessionService.Setup(s => s.RevokeSessionAsync(command.SessionToken, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Sesión cerrada exitosamente", result.Message);
            _mockSessionService.Verify(s => s.RevokeSessionAsync(command.SessionToken, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidSessionToken_ShouldReturnNotSuccessAndNotRevoke()
        {
            // Arrange
            var command = CreateLogoutCommand("invalid_token");
            _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(It.IsAny<Expression<Func<Session, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Sesión no encontrada o ya cerrada", result.Message);
            _mockSessionService.Verify(s => s.RevokeSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SessionNotActive_ShouldReturnNotSuccessAndNotRevoke()
        {
            // Arrange
            var command = CreateLogoutCommand();
            var user = CreateStubUser();
            var session = CreateTestSession(command.SessionToken, user, SessionStatus.Expired); // Not active

            _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(It.IsAny<Expression<Func<Session, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("La sesión ya está cerrada", result.Message);
            _mockSessionService.Verify(s => s.RevokeSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
