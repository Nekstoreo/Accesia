using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Accesia.Infrastructure.Services;
using Accesia.Infrastructure.Data; // Assuming ApplicationDbContext is here
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Accesia.Infrastructure.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias/de integración ligera para <see cref="SessionService"/>.
    /// Estas pruebas utilizan una base de datos EF Core InMemory para verificar la lógica del servicio
    /// que interactúa directamente con la base de datos, incluyendo la creación, recuperación,
    /// actualización y revocación de entidades <see cref="Session"/>.
    /// El uso de InMemoryDatabase permite probar queries de EF Core y la lógica de
    /// persistencia de forma más realista que con mocks completos del DbContext.
    /// </summary>
    public class SessionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SessionService _sessionService;
        private readonly Mock<ILogger<SessionService>> _mockLogger;
        private readonly User _testUser;

        public SessionServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test run
                .Options;
            _dbContext = new ApplicationDbContext(options); // Assuming ApplicationDbContext takes DbContextOptions
            _mockLogger = new Mock<ILogger<SessionService>>();
            _sessionService = new SessionService(_dbContext, _mockLogger.Object);

            // Seed a test user
            _testUser = User.CreateNewUser(new Email("test@example.com"), "hashedpassword", "Test", "User");
            _dbContext.Users.Add(_testUser);
            _dbContext.SaveChanges();
        }

        private DeviceInfo CreateTestDeviceInfo() =>
            new DeviceInfo { UserAgent = "TestAgent/1.0", Browser = "TestBrowser", OperatingSystem = "TestOS", DeviceFingerprint = "fp123" };

        private LocationInfo CreateTestLocationInfo() =>
            new LocationInfo { IpAddress = "127.0.0.1", City = "TestCity", Country = "TestCountry" };

        [Fact]
        public async Task CreateSessionAsync_ValidUserAndInfo_ShouldCreateAndSaveSession()
        {
            // Arrange
            var deviceInfo = CreateTestDeviceInfo();
            var locationInfo = CreateTestLocationInfo();
            var loginMethod = "Password";

            // Act
            var session = await _sessionService.CreateSessionAsync(_testUser, deviceInfo, locationInfo, loginMethod, CancellationToken.None);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(_testUser.Id, session.UserId);
            Assert.Equal(deviceInfo.UserAgent, session.UserAgent);
            Assert.Equal(locationInfo.IpAddress, session.InitialIpAddress);
            Assert.Equal(SessionStatus.Active, session.Status);
            Assert.False(session.IsKnownDevice); // First session for this device

            var savedSession = await _dbContext.Sessions.FindAsync(session.Id);
            Assert.NotNull(savedSession);
            Assert.Equal(session.SessionToken, savedSession.SessionToken);
        }

        [Fact]
        public async Task GetSessionByTokenAsync_ExistingToken_ShouldReturnSessionWithUser()
        {
            // Arrange
            var createdSession = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");

            // Act
            var fetchedSession = await _sessionService.GetSessionByTokenAsync(createdSession.SessionToken, CancellationToken.None);

            // Assert
            Assert.NotNull(fetchedSession);
            Assert.Equal(createdSession.Id, fetchedSession.Id);
            Assert.NotNull(fetchedSession.User);
            Assert.Equal(_testUser.Id, fetchedSession.User.Id);
        }

        [Fact]
        public async Task GetSessionByTokenAsync_NonExistingToken_ShouldReturnNull()
        {
            // Act
            var session = await _sessionService.GetSessionByTokenAsync("non_existing_token", CancellationToken.None);

            // Assert
            Assert.Null(session);
        }

        [Fact]
        public async Task RefreshSessionAsync_ValidRefreshToken_ShouldUpdateSessionAndReturnIt()
        {
            // Arrange
            var originalSession = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            originalSession.ExpiresAt = DateTime.UtcNow.AddMinutes(5); // Ensure it's active but refreshable
            originalSession.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1);
            _dbContext.SaveChanges();
            var originalRefreshToken = originalSession.RefreshToken;

            // Act
            var refreshedSession = await _sessionService.RefreshSessionAsync(originalRefreshToken, CancellationToken.None);

            // Assert
            Assert.NotNull(refreshedSession);
            Assert.NotEqual(originalRefreshToken, refreshedSession.RefreshToken); // New refresh token generated
            Assert.True(refreshedSession.LastActivityAt > originalSession.LastActivityAt);
            Assert.Equal(originalSession.Id, refreshedSession.Id); // Should be the same session entity, updated
        }

        [Fact]
        public async Task RefreshSessionAsync_InvalidOrExpiredRefreshToken_ShouldReturnNull()
        {
            // Arrange
            var session = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            session.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Expired refresh token
            _dbContext.SaveChanges();

            // Act
            var refreshedSession = await _sessionService.RefreshSessionAsync(session.RefreshToken, CancellationToken.None);
            var refreshedSessionInvalid = await _sessionService.RefreshSessionAsync("invalid_refresh_token", CancellationToken.None);


            // Assert
            Assert.Null(refreshedSession);
            Assert.Null(refreshedSessionInvalid);
        }


        [Fact]
        public async Task RevokeSessionAsync_ActiveSession_ShouldSetStatusToRevoked()
        {
            // Arrange
            var session = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            Assert.Equal(SessionStatus.Active, session.Status);

            // Act
            await _sessionService.RevokeSessionAsync(session.SessionToken, CancellationToken.None);

            // Assert
            var revokedSession = await _dbContext.Sessions.FindAsync(session.Id);
            Assert.NotNull(revokedSession);
            Assert.Equal(SessionStatus.Revoked, revokedSession.Status);
        }

        [Fact]
        public async Task RevokeAllUserSessionsAsync_ExistingUser_ShouldRevokeAllActiveSessionsForUser()
        {
            // Arrange
            var session1 = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            var session2 = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            var otherUser = User.CreateNewUser(new Email("other@example.com"), "hash", "Other", "User");
            _dbContext.Users.Add(otherUser);
            await _dbContext.SaveChangesAsync();
            var sessionOtherUser = await _sessionService.CreateSessionAsync(otherUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");


            // Act
            await _sessionService.RevokeAllUserSessionsAsync(_testUser.Id, CancellationToken.None);

            // Assert
            var sessionsUser1 = await _dbContext.Sessions.Where(s => s.UserId == _testUser.Id).ToListAsync();
            Assert.All(sessionsUser1, s => Assert.Equal(SessionStatus.Revoked, s.Status));

            var sessionOtherUserDb = await _dbContext.Sessions.FindAsync(sessionOtherUser.Id);
            Assert.NotNull(sessionOtherUserDb);
            Assert.Equal(SessionStatus.Active, sessionOtherUserDb.Status); // Ensure other user's sessions are not affected
        }

        [Fact]
        public async Task ValidateSessionAsync_ActiveSessionToken_ShouldReturnTrue()
        {
            // Arrange
            var session = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            session.ExpiresAt = DateTime.UtcNow.AddHours(1); // Ensure it's not expired
            await _dbContext.SaveChangesAsync();


            // Act
            var isValid = await _sessionService.ValidateSessionAsync(session.SessionToken, CancellationToken.None);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateSessionAsync_InactiveOrExpiredSessionToken_ShouldReturnFalse()
        {
            // Arrange
            var activeSession = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            activeSession.ExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Expired
            await _dbContext.SaveChangesAsync();

            var revokedSession = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            revokedSession.Revoke();
            await _dbContext.SaveChangesAsync();


            // Act
            var isExpiredValid = await _sessionService.ValidateSessionAsync(activeSession.SessionToken, CancellationToken.None);
            var isRevokedValid = await _sessionService.ValidateSessionAsync(revokedSession.SessionToken, CancellationToken.None);
            var isNonExistentValid = await _sessionService.ValidateSessionAsync("non_existent_token", CancellationToken.None);


            // Assert
            Assert.False(isExpiredValid);
            Assert.False(isRevokedValid);
            Assert.False(isNonExistentValid);
        }


        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithExpiredSessions_ShouldMarkThemAsExpired()
        {
            // Arrange
            var activeSession = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            activeSession.ExpiresAt = DateTime.UtcNow.AddHours(1); // Stays active

            var expiredSession1 = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            expiredSession1.ExpiresAt = DateTime.UtcNow.AddMinutes(-10); // Should be expired by time

            var expiredSession2 = await _sessionService.CreateSessionAsync(_testUser, CreateTestDeviceInfo(), CreateTestLocationInfo(), "Password");
            expiredSession2.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Should be expired by refresh token time

            await _dbContext.SaveChangesAsync();
            Assert.Equal(SessionStatus.Active, expiredSession1.Status); // Verify initial state
            Assert.Equal(SessionStatus.Active, expiredSession2.Status);


            // Act
            await _sessionService.CleanupExpiredSessionsAsync(CancellationToken.None);

            // Assert
            var dbActiveSession = await _dbContext.Sessions.FindAsync(activeSession.Id);
            var dbExpiredSession1 = await _dbContext.Sessions.FindAsync(expiredSession1.Id);
            var dbExpiredSession2 = await _dbContext.Sessions.FindAsync(expiredSession2.Id);

            Assert.NotNull(dbActiveSession);
            Assert.Equal(SessionStatus.Active, dbActiveSession.Status);

            Assert.NotNull(dbExpiredSession1);
            Assert.Equal(SessionStatus.Expired, dbExpiredSession1.Status);

            Assert.NotNull(dbExpiredSession2);
            Assert.Equal(SessionStatus.Expired, dbExpiredSession2.Status);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenDeviceIsKnown_SetsIsKnownDeviceToTrue()
        {
            // Arrange
            var deviceInfo = CreateTestDeviceInfo(); // Same device fingerprint
            var locationInfo = CreateTestLocationInfo();

            // First session - device is unknown
            await _sessionService.CreateSessionAsync(_testUser, deviceInfo, locationInfo, "Password");

            // Act: Second session with the same device fingerprint for the same user
            var secondSession = await _sessionService.CreateSessionAsync(_testUser, deviceInfo, locationInfo, "Password");

            // Assert
            Assert.True(secondSession.IsKnownDevice);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenDeviceIsUnknownForUser_SetsIsKnownDeviceToFalse()
        {
            // Arrange
            var deviceInfo1 = new DeviceInfo { UserAgent = "Agent1", Browser="B1", OperatingSystem="OS1", DeviceFingerprint = "fingerprint_A" };
            var deviceInfo2 = new DeviceInfo { UserAgent = "Agent2", Browser="B2", OperatingSystem="OS2", DeviceFingerprint = "fingerprint_B" }; // Different fingerprint
            var locationInfo = CreateTestLocationInfo();

            await _sessionService.CreateSessionAsync(_testUser, deviceInfo1, locationInfo, "Password");

            // Act
            var secondSessionWithDifferentDevice = await _sessionService.CreateSessionAsync(_testUser, deviceInfo2, locationInfo, "Password");

            // Assert
            Assert.False(secondSessionWithDifferentDevice.IsKnownDevice);
        }


        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted(); // Clean up the in-memory database after each test
            _dbContext.Dispose();
        }
    }
}
