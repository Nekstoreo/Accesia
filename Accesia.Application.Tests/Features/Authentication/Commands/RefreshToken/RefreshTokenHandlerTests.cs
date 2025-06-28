using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.RefreshToken;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Application.Common.Exceptions;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums; // Required for SessionStatus
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Collections.Generic; // For IEnumerable in GenerateAccessToken mock

namespace Accesia.Application.Tests.Features.Authentication.Commands.RefreshToken
{
    /// <summary>
    /// Pruebas unitarias para <see cref="RefreshTokenHandler"/>.
    /// </summary>
    public class RefreshTokenHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IDeviceInfoService> _mockDeviceInfoService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<ILogger<RefreshTokenHandler>> _mockLogger;
        private readonly RefreshTokenHandler _handler;

        public RefreshTokenHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockSessionService = new Mock<ISessionService>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockDeviceInfoService = new Mock<IDeviceInfoService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockLogger = new Mock<ILogger<RefreshTokenHandler>>();

            _handler = new RefreshTokenHandler(
                _mockContext.Object,
                _mockSessionService.Object,
                _mockJwtTokenService.Object,
                _mockDeviceInfoService.Object,
                _mockRateLimitService.Object,
                _mockLogger.Object
            );

            // Default rate limit allow
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        private RefreshTokenCommand CreateCommand(string token = "valid_refresh_token", string ip = "127.0.0.1", string ua = "TestAgent")
        {
            return new RefreshTokenCommand { RefreshToken = token, IpAddress = ip, UserAgent = ua };
        }

        private User CreateTestUser(Guid? id = null, string email = "test@example.com")
        {
            var user = User.CreateNewUser(new Email(email), "hashed_password", "Test", "User");
            if (id.HasValue) user.Id = id.Value;
            // Add default role for token generation if necessary
            var defaultRole = new Role { Name = "User", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission>() };
            user.UserRoles = new List<UserRole> { new UserRole { UserId = user.Id, RoleId = defaultRole.Id, Role = defaultRole, IsActive = true } };
            return user;
        }

        private Session CreateTestSession(User user, string refreshToken, bool canBeRefreshed = true, DeviceInfo? deviceInfo = null, LocationInfo? locationInfo = null)
        {
            var session = Session.CreateNewSession(
                user,
                deviceInfo ?? new DeviceInfo { UserAgent = "OriginalAgent", Browser = "OriginalBrowser", OperatingSystem = "OriginalOS", DeviceFingerprint = "fp_orig" },
                locationInfo ?? new LocationInfo { IpAddress = "192.168.0.1", City = "OriginalCity", Country = "OriginalCountry" },
                "Password"
            );
            session.RefreshToken = refreshToken;
            session.User = user; // Ensure User navigation property is set for the handler
            session.UserId = user.Id;

            if (!canBeRefreshed)
            {
                session.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(-10); // Expired
            }
            else
            {
                session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1); // Valid
            }
            session.Status = SessionStatus.Active; // Must be active to be refreshable by SessionService
            return session;
        }

        [Fact]
        public async Task Handle_ValidRefreshToken_ShouldReturnNewTokensAndSessionInfo()
        {
            // Arrange
            var command = CreateCommand();
            var user = CreateTestUser();
            var originalSession = CreateTestSession(user, command.RefreshToken, canBeRefreshed: true);

            var refreshedSession = CreateTestSession(user, "new_refreshed_token_from_service"); // Simulate SessionService returning a session with new RT
            refreshedSession.Id = originalSession.Id; // Important: SessionService.RefreshSessionAsync updates existing session
            refreshedSession.DeviceInfo = new DeviceInfo { UserAgent = command.UserAgent, Browser = "NewBrowser", OperatingSystem = "NewOS", DeviceFingerprint = "fp_new" }; // Simulate DeviceInfoService
            refreshedSession.LocationInfo = new LocationInfo { IpAddress = command.IpAddress, City = "NewCity", Country = "NewCountry" }; // Simulate DeviceInfoService


            _mockSessionService.Setup(s => s.RefreshSessionAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshedSession); // Simulate SessionService refreshing successfully and returning the updated session

            _mockDeviceInfoService.Setup(s => s.ExtractDeviceInfo(command.UserAgent)).Returns(refreshedSession.DeviceInfo);
            _mockDeviceInfoService.Setup(s => s.ExtractLocationInfo(command.IpAddress)).Returns(refreshedSession.LocationInfo);

            var newAccessToken = "new_jwt_access_token";
            var tokenExpiration = DateTime.UtcNow.AddHours(1);
            _mockJwtTokenService.Setup(s => s.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns(newAccessToken);
            _mockJwtTokenService.Setup(s => s.GetTokenExpiration()).Returns(tokenExpiration);

            // The handler fetches the user from the refreshedSession.User
            // _mockContext.Setup(c => c.Users.FindAsync(new object[] { user.Id }, It.IsAny<CancellationToken>())).ReturnsAsync(user);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newAccessToken, result.AccessToken);
            Assert.Equal(refreshedSession.RefreshToken, result.RefreshToken); // RT from the refreshed session by service
            Assert.Equal(user.Id, result.User.Id);
            Assert.Equal(refreshedSession.SessionToken, result.Session.SessionId);

            _mockSessionService.Verify(s => s.RefreshSessionAsync(command.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockJwtTokenService.Verify(s => s.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Once);
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.IpAddress, "refresh_token", It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // For updating session with new device/location info
        }

        [Fact]
        public async Task Handle_SessionServiceReturnsNull_ShouldThrowInvalidVerificationTokenException()
        {
            // Arrange
            var command = CreateCommand("invalid_or_expired_token");
            _mockSessionService.Setup(s => s.RefreshSessionAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null); // Simulate session not found or not refreshable

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(command.RefreshToken, exception.Token);
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.IpAddress, "refresh_token", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_RefreshedSessionUserIsNull_ShouldThrowUserNotFoundException()
        {
            // Arrange
            var command = CreateCommand();
            var sessionWithNullUser = new Session // Manual construction for test case
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // Has a UserId
                User = null, // User navigation property is null
                RefreshToken = "some_new_rt",
                SessionToken = "session_token",
                Status = SessionStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
                DeviceInfo = new DeviceInfo(), LocationInfo = new LocationInfo()
            };

            _mockSessionService.Setup(s => s.RefreshSessionAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionWithNullUser);
            _mockDeviceInfoService.Setup(s => s.ExtractDeviceInfo(command.UserAgent)).Returns(new DeviceInfo());
            _mockDeviceInfoService.Setup(s => s.ExtractLocationInfo(command.IpAddress)).Returns(new LocationInfo());


            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(command.IpAddress, "refresh_token", It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Handle_RateLimitExceeded_ShouldThrowRateLimitExceededException()
        {
            // Arrange
            var command = CreateCommand("token_for_rate_limit_test");
            var cooldown = TimeSpan.FromSeconds(45);
            _mockRateLimitService.Setup(s => s.CanPerformActionAsync(command.IpAddress, "refresh_token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimitService.Setup(s => s.GetRemainingCooldownAsync(command.IpAddress, "refresh_token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cooldown);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(cooldown, exception.RetryAfter);
            _mockRateLimitService.Verify(r => r.RecordActionAttemptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DeviceInfoOrLocationInfoChanges_ShouldUpdateSessionInDbContext()
        {
            // Arrange
            var command = CreateCommand(ip: "192.168.1.100", ua: "UpdatedAgent/2.0");
            var user = CreateTestUser();
            var originalDeviceInfo = new DeviceInfo { UserAgent = "OriginalAgent", Browser = "Firefox", OperatingSystem = "Linux", DeviceFingerprint = "fp_orig" };
            var originalLocationInfo = new LocationInfo { IpAddress = "10.0.0.5", City = "OldCity", Country = "OldCountry" };

            var session = CreateTestSession(user, command.RefreshToken, deviceInfo: originalDeviceInfo, locationInfo: originalLocationInfo);
            session.IsKnownDevice = true; // Assume it was a known device initially

            var newDeviceInfoFromService = new DeviceInfo { UserAgent = command.UserAgent, Browser = "Chrome", OperatingSystem = "Windows", DeviceFingerprint = "fp_new" };
            var newLocationInfoFromService = new LocationInfo { IpAddress = command.IpAddress, City = "NewCity", Country = "NewCountry" };

            _mockSessionService.Setup(s => s.RefreshSessionAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session); // Return the same session instance, which the handler will modify
            _mockDeviceInfoService.Setup(s => s.ExtractDeviceInfo(command.UserAgent)).Returns(newDeviceInfoFromService);
            _mockDeviceInfoService.Setup(s => s.ExtractLocationInfo(command.IpAddress)).Returns(newLocationInfoFromService);
            _mockJwtTokenService.Setup(s => s.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns("new_access_token");
            _mockJwtTokenService.Setup(s => s.GetTokenExpiration()).Returns(DateTime.UtcNow.AddHours(1));


            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verify that the session's DeviceInfo and LocationInfo were updated
            Assert.Equal(newDeviceInfoFromService.UserAgent, session.DeviceInfo.UserAgent);
            Assert.Equal(newDeviceInfoFromService.Browser, session.DeviceInfo.Browser);
            Assert.Equal(newLocationInfoFromService.IpAddress, session.LocationInfo.IpAddress);
            Assert.Equal(newLocationInfoFromService.City, session.LocationInfo.City);

            // Verify IsKnownDevice is set to false because DeviceFingerprint changed
            // (This depends on the logic in DeviceInfoService and how Session.UpdateLocation handles it,
            // but RefreshTokenHandler updates it if DeviceInfo or LocationInfo changes)
            Assert.False(session.IsKnownDevice);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}
