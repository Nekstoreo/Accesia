using Xunit;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System;

namespace Accesia.Domain.Tests.Entities
{
    public class SessionTests
    {
        private User CreateStubUser() => User.CreateNewUser(new Email("user@example.com"), "hash", "User", "Test");
        private DeviceInfo CreateTestDeviceInfo() => new DeviceInfo { UserAgent = "TestAgent", Browser = "Chrome", OperatingSystem = "Win10", DeviceFingerprint = "fp1" };
        private LocationInfo CreateTestLocationInfo() => new LocationInfo { IpAddress = "192.168.1.1", City = "Testville", Country = "Testland" };

        private Session CreateTestSession(User? user = null, SessionStatus initialStatus = SessionStatus.Active, DateTime? expiresAt = null)
        {
            var testUser = user ?? CreateStubUser();
            var session = Session.CreateNewSession(
                testUser,
                CreateTestDeviceInfo(),
                CreateTestLocationInfo(),
                "Password" // LoginMethod string
            );
            session.Status = initialStatus;
            session.ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1);
            session.RefreshTokenExpiresAt = expiresAt?.AddDays(6) ?? DateTime.UtcNow.AddDays(7);
            return session;
        }

        [Fact]
        public void CreateNewSession_ValidParameters_InitializesCorrectly()
        {
            // Arrange
            var user = CreateStubUser();
            var deviceInfo = CreateTestDeviceInfo();
            var locationInfo = CreateTestLocationInfo();
            var loginMethod = "Password"; // Corresponds to LoginMethod.Password

            // Act
            var session = Session.CreateNewSession(user, deviceInfo, locationInfo, loginMethod);

            // Assert
            Assert.Equal(user.Id, session.UserId);
            Assert.NotNull(session.SessionToken);
            Assert.NotEmpty(session.SessionToken);
            Assert.NotNull(session.RefreshToken);
            Assert.NotEmpty(session.RefreshToken);
            Assert.Equal(SessionStatus.Active, session.Status);
            Assert.True(session.ExpiresAt > DateTime.UtcNow);
            Assert.True(session.RefreshTokenExpiresAt > DateTime.UtcNow);
            Assert.Equal(DateTime.UtcNow, session.LastActivityAt, TimeSpan.FromSeconds(5)); // Allow small delta
            Assert.Equal(deviceInfo, session.DeviceInfo);
            Assert.Equal(locationInfo, session.LocationInfo);
            Assert.False(session.IsKnownDevice); // Default for new session
            Assert.Equal(LoginMethod.Password, session.LoginMethod);
            Assert.False(session.MfaVerified);
            Assert.Equal(0, session.RiskScore);
            Assert.Equal(deviceInfo.UserAgent, session.UserAgent);
            Assert.Equal(locationInfo.IpAddress, session.InitialIpAddress);
            Assert.Equal(locationInfo.IpAddress, session.LastIpAddress);
        }

        [Fact]
        public void Activate_WhenInactive_SetsStatusToActiveAndUpdateActivity()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Revoked);
            var initialActivity = session.LastActivityAt;

            // Act
            session.Activate();

            // Assert
            Assert.Equal(SessionStatus.Active, session.Status);
            Assert.True(session.LastActivityAt > initialActivity);
        }

        [Fact]
        public void Activate_WhenAlreadyActive_DoesNotChangeStatusOrActivityTimeSignificantly()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active);
            var initialActivity = session.LastActivityAt;
            var initialStatus = session.Status;

            // Introduce a small delay to ensure LastActivityAt would change if updated
            System.Threading.Thread.Sleep(10);


            // Act
            session.Activate(); // Should not change if already active

            // Assert
            Assert.Equal(initialStatus, session.Status);
            // LastActivityAt should NOT be updated if it was already active and Activate() was called again
            // The current implementation of Activate() updates LastActivityAt only if status was NOT Active.
            // Let's check the logic: `if (Status != SessionStatus.Active)` then update. So this is correct.
            Assert.Equal(initialActivity, session.LastActivityAt);
        }


        [Fact]
        public void Expire_SetsStatusToExpiredAndUpdateActivity()
        {
            // Arrange
            var session = CreateTestSession();
            var initialActivity = session.LastActivityAt;

            // Act
            session.Expire();

            // Assert
            Assert.Equal(SessionStatus.Expired, session.Status);
            Assert.True(session.LastActivityAt > initialActivity);
        }

        [Fact]
        public void Revoke_SetsStatusToRevokedAndUpdateActivity()
        {
            // Arrange
            var session = CreateTestSession();
            var initialActivity = session.LastActivityAt;

            // Act
            session.Revoke();

            // Assert
            Assert.Equal(SessionStatus.Revoked, session.Status);
            Assert.True(session.LastActivityAt > initialActivity);
        }

        [Fact]
        public void UpdateLastActivity_UpdatesActivityTimestamp()
        {
            // Arrange
            var session = CreateTestSession();
            var initialActivity = session.LastActivityAt;
            System.Threading.Thread.Sleep(10); // Ensure time passes

            // Act
            session.UpdateLastActivity();

            // Assert
            Assert.True(session.LastActivityAt > initialActivity);
        }

        [Fact]
        public void ExtendExpiration_WhenActive_ExtendsExpiresAtAndRefreshTokenExpiresAt()
        {
            // Arrange
            var session = CreateTestSession();
            var originalExpiresAt = session.ExpiresAt;
            var originalRefreshExpiresAt = session.RefreshTokenExpiresAt;
            var extension = TimeSpan.FromHours(1);

            // Act
            session.ExtendExpiration(extension);

            // Assert
            Assert.Equal(originalExpiresAt.Add(extension), session.ExpiresAt);
            Assert.Equal(originalRefreshExpiresAt.Add(extension), session.RefreshTokenExpiresAt);
        }

        [Fact]
        public void ExtendExpiration_WhenNotActive_DoesNotExtend()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Expired);
            var originalExpiresAt = session.ExpiresAt;
            var originalRefreshExpiresAt = session.RefreshTokenExpiresAt;
            var extension = TimeSpan.FromHours(1);

            // Act
            session.ExtendExpiration(extension);

            // Assert
            Assert.Equal(originalExpiresAt, session.ExpiresAt);
            Assert.Equal(originalRefreshExpiresAt, session.RefreshTokenExpiresAt);
        }

        [Fact]
        public void IsActive_WhenActiveAndNotExpired_ReturnsTrue()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active, expiresAt: DateTime.UtcNow.AddMinutes(30));

            // Assert
            Assert.True(session.IsActive());
        }

        [Fact]
        public void IsActive_WhenExpired_ReturnsFalse()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active, expiresAt: DateTime.UtcNow.AddMinutes(-30));

            // Assert
            Assert.False(session.IsActive());
        }

        [Fact]
        public void IsActive_WhenStatusNotActive_ReturnsFalse()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Revoked, expiresAt: DateTime.UtcNow.AddMinutes(30));

            // Assert
            Assert.False(session.IsActive());
        }

        [Fact]
        public void IsExpired_WhenStatusIsExpired_ReturnsTrue()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Expired);

            // Act & Assert
            Assert.True(session.IsExpired());
        }

        [Fact]
        public void IsExpired_WhenTimeIsPastExpiresAt_ReturnsTrue()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active, expiresAt: DateTime.UtcNow.AddMilliseconds(-100));

            // Act & Assert
            Assert.True(session.IsExpired());
        }


        [Fact]
        public void CanBeRefreshed_WhenActiveAndRefreshTokenNotExpired_ReturnsTrue()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active);
            session.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);

            // Assert
            Assert.True(session.CanBeRefreshed());
        }

        [Fact]
        public void CanBeRefreshed_WhenRefreshTokenExpired_ReturnsFalse()
        {
            // Arrange
            var session = CreateTestSession(initialStatus: SessionStatus.Active);
            session.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(-30);

            // Assert
            Assert.False(session.CanBeRefreshed());
        }

        [Fact]
        public void GenerateNewRefreshToken_UpdatesTokenAndItsExpiry()
        {
            // Arrange
            var session = CreateTestSession();
            var oldRefreshToken = session.RefreshToken;
            var oldRefreshTokenExpiry = session.RefreshTokenExpiresAt;

            // Act
            session.GenerateNewRefreshToken();

            // Assert
            Assert.NotEqual(oldRefreshToken, session.RefreshToken);
            Assert.True(session.RefreshTokenExpiresAt > oldRefreshTokenExpiry);
            Assert.True(session.RefreshTokenExpiresAt > DateTime.UtcNow);
        }

        [Theory]
        [InlineData("password", LoginMethod.Password)]
        [InlineData("oauth", LoginMethod.GoogleOAuth)] // Based on current ParseLoginMethod logic
        [InlineData("mfa", LoginMethod.MFA)]
        [InlineData("sso", LoginMethod.SAML)]
        [InlineData("GOOGLE", LoginMethod.GoogleOAuth)] // Test case-insensitivity if ParseLoginMethod supports it
        [InlineData("unknown_method", LoginMethod.Password)] // Default
        public void CreateNewSession_ParsesLoginMethodCorrectly(string loginMethodString, LoginMethod expectedMethod)
        {
            // Arrange
            var user = CreateStubUser();

            // Act
            var session = Session.CreateNewSession(user, CreateTestDeviceInfo(), CreateTestLocationInfo(), loginMethodString);

            // Assert
            Assert.Equal(expectedMethod, session.LoginMethod);
        }
    }
}
