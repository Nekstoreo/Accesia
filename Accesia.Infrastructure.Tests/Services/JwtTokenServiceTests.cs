using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Accesia.Infrastructure.Services;
using Accesia.Application.Common.Settings; // For JwtSettings
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Accesia.Infrastructure.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private readonly Mock<ILogger<JwtTokenService>> _mockLogger;
        private readonly JwtSettings _jwtSettings;
        private readonly JwtTokenService _jwtTokenService;
        private readonly User _testUser;

        public JwtTokenServiceTests()
        {
            _mockLogger = new Mock<ILogger<JwtTokenService>>();
            _jwtSettings = new JwtSettings // Use a consistent, testable configuration
            {
                Key = "Str0ngS3cr3tK3yF0rT3st1ngP&rp0s3s!12345", // Must be long enough for HS256
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiresInMinutes = 60
            };
            var mockOptions = new Mock<IOptions<JwtSettings>>();
            mockOptions.Setup(o => o.Value).Returns(_jwtSettings);

            _jwtTokenService = new JwtTokenService(mockOptions.Object, _mockLogger.Object);

            _testUser = User.CreateNewUser(new Email("test@example.com"), "hashedpassword", "Test", "User");
            _testUser.Id = Guid.NewGuid(); // Ensure Id is set
        }

        [Fact]
        public void GenerateAccessToken_ValidUser_ShouldReturnValidJwtToken()
        {
            // Arrange
            var roles = new List<string> { "User", "Editor" };
            var permissions = new List<string> { "read:articles", "write:articles" };

            // Act
            var tokenString = _jwtTokenService.GenerateAccessToken(_testUser, roles, permissions);

            // Assert
            Assert.NotNull(tokenString);
            Assert.NotEmpty(tokenString);

            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(tokenString);

            Assert.Equal(_jwtSettings.Issuer, decodedToken.Issuer);
            Assert.Equal(_jwtSettings.Audience, decodedToken.Audiences.First());

            Assert.Equal(_testUser.Id.ToString(), decodedToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal(_testUser.Email.Value, decodedToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal(_testUser.FirstName, decodedToken.Claims.First(c => c.Type == ClaimTypes.GivenName).Value);
            Assert.Equal(_testUser.LastName, decodedToken.Claims.First(c => c.Type == ClaimTypes.Surname).Value);

            var roleClaims = decodedToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.Equal(roles.Count, roleClaims.Count);
            foreach (var role in roles)
            {
                Assert.Contains(role, roleClaims);
            }

            var permissionClaims = decodedToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
            Assert.Equal(permissions.Count, permissionClaims.Count);
            foreach (var perm in permissions)
            {
                Assert.Contains(perm, permissionClaims);
            }

            var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);
            Assert.True(decodedToken.ValidTo > DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes -1)); // Allow slight timing differences
            Assert.True(decodedToken.ValidTo <= expectedExpiry.AddSeconds(5)); // Check upper bound with a small tolerance
        }

        [Fact]
        public void GenerateAccessToken_UserWithNoRolesOrPermissions_ShouldGenerateTokenWithoutRoleOrPermissionClaims()
        {
            // Arrange
            var roles = new List<string>();
            var permissions = new List<string>();

            // Act
            var tokenString = _jwtTokenService.GenerateAccessToken(_testUser, roles, permissions);
            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(tokenString);

            // Assert
            Assert.DoesNotContain(decodedToken.Claims, c => c.Type == ClaimTypes.Role);
            Assert.DoesNotContain(decodedToken.Claims, c => c.Type == "permission");
        }


        [Fact]
        public void GetTokenExpiration_ShouldReturnCorrectExpirationDateTime()
        {
            // Arrange
            // Act
            var expirationTime = _jwtTokenService.GetTokenExpiration();
            var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

            // Assert
            // Allow a small delta for the time difference between calculating expected and actual
            Assert.True(expirationTime >= expectedExpiration.AddSeconds(-5) && expirationTime <= expectedExpiration.AddSeconds(5));
        }

        [Fact]
        public void GenerateAccessToken_WithNullUser_ShouldThrowArgumentNullException()
        {
            // Arrange
            User nullUser = null;
            var roles = new List<string> { "User" };
            var permissions = new List<string> { "read" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jwtTokenService.GenerateAccessToken(nullUser, roles, permissions));
        }

        [Fact]
        public void GenerateAccessToken_WithNullRoles_ShouldThrowArgumentNullException()
        {
            // Arrange
            List<string> nullRoles = null;
            var permissions = new List<string> { "read" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jwtTokenService.GenerateAccessToken(_testUser, nullRoles, permissions));
        }

        [Fact]
        public void GenerateAccessToken_WithNullPermissions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var roles = new List<string> { "User" };
            List<string> nullPermissions = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jwtTokenService.GenerateAccessToken(_testUser, roles, nullPermissions));
        }
    }
}
