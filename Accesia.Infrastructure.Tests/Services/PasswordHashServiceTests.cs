using Xunit;
using Accesia.Infrastructure.Services; // Assuming PasswordHashService is here
using Microsoft.Extensions.Logging;
using Moq;

namespace Accesia.Infrastructure.Tests.Services
{
    public class PasswordHashServiceTests
    {
        private readonly PasswordHashService _passwordHashService;
        private readonly Mock<ILogger<PasswordHashService>> _mockLogger;

        public PasswordHashServiceTests()
        {
            _mockLogger = new Mock<ILogger<PasswordHashService>>();
            // PasswordHashService might take IOptions<PasswordSettings> or similar if configurable
            // For BCrypt, it's often not configured via IOptions unless work factor is externalized.
            _passwordHashService = new PasswordHashService(_mockLogger.Object);
        }

        [Fact]
        public void HashPassword_ValidPassword_ShouldReturnNonEmptyHash()
        {
            // Arrange
            var password = "mysecretpassword";

            // Act
            var hashedPassword = _passwordHashService.HashPassword(password);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.NotEqual(password, hashedPassword); // Hash should not be the same as the password
        }

        [Fact]
        public void VerifyPassword_CorrectPasswordAndHash_ShouldReturnTrue()
        {
            // Arrange
            var password = "mysecretpassword123";
            var hashedPassword = _passwordHashService.HashPassword(password); // Generate a hash

            // Act
            var isPasswordCorrect = _passwordHashService.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.True(isPasswordCorrect);
        }

        [Fact]
        public void VerifyPassword_IncorrectPasswordAndHash_ShouldReturnFalse()
        {
            // Arrange
            var correctPassword = "mysecretpasswordABC";
            var incorrectPassword = "wrongpasswordXYZ";
            var hashedPassword = _passwordHashService.HashPassword(correctPassword);

            // Act
            var isPasswordCorrect = _passwordHashService.VerifyPassword(incorrectPassword, hashedPassword);

            // Assert
            Assert.False(isPasswordCorrect);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")] // Whitespace only
        public void HashPassword_NullOrEmptyOrWhitespacePassword_ShouldThrowArgumentException(string password)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _passwordHashService.HashPassword(password));
            Assert.Contains("Password cannot be null or empty or whitespace.", exception.Message);
        }

        [Theory]
        [InlineData(null, "somehash")]
        [InlineData("", "somehash")]
        [InlineData("   ", "somehash")]
        [InlineData("somepassword", null)]
        [InlineData("somepassword", "")]
        [InlineData("somepassword", "   ")]
        public void VerifyPassword_NullOrEmptyOrWhitespaceInputs_ShouldReturnFalseOrThrow(string password, string hash)
        {
            // BCrypt's Verify typically throws if hash is invalid format or password is null.
            // Let's test for ArgumentException for null/empty password as per our HashPassword,
            // and expect false for invalid hash, though BCrypt lib might throw for malformed hash.

            if (string.IsNullOrWhiteSpace(password))
            {
                 var exception = Assert.Throws<ArgumentException>(() => _passwordHashService.VerifyPassword(password, hash));
                 Assert.Contains("Password cannot be null or empty or whitespace.", exception.Message);

            }
            else if (string.IsNullOrWhiteSpace(hash))
            {
                 var exception = Assert.Throws<ArgumentException>(() => _passwordHashService.VerifyPassword(password, hash));
                 Assert.Contains("Hash cannot be null or empty or whitespace.", exception.Message);
            }
            else // Valid password, invalid hash format (though BCrypt lib handles this by returning false)
            {
                 // This case depends on BCrypt.Net behavior for malformed hashes.
                 // Typically, it might throw a SaltParseException or return false.
                 // For this test, if it doesn't throw, we expect false.
                 try
                 {
                    Assert.False(_passwordHashService.VerifyPassword(password, "invalidhashformat"));
                 }
                 catch (Exception ex) when (ex.GetType().Name == "SaltParseException") // Specific to BCrypt.Net
                 {
                    // This is also an acceptable outcome if the library throws for malformed hash
                    Assert.True(true, "BCrypt library threw SaltParseException for invalid hash format, which is acceptable.");
                 }
            }
        }

        [Fact]
        public void HashPassword_DifferentPasswords_ShouldProduceDifferentHashes()
        {
            // Arrange
            var passwordA = "PasswordA1!";
            var passwordB = "PasswordB2@";

            // Act
            var hashA = _passwordHashService.HashPassword(passwordA);
            var hashB = _passwordHashService.HashPassword(passwordB);

            // Assert
            Assert.NotEqual(hashA, hashB);
        }

        [Fact]
        public void HashPassword_SamePasswordMultipleTimes_ShouldProduceDifferentHashesDueToSalt()
        {
            // Arrange
            var password = "CommonPassword#3";

            // Act
            var hash1 = _passwordHashService.HashPassword(password);
            var hash2 = _passwordHashService.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // BCrypt includes a salt, so hashes of the same input differ
            Assert.True(_passwordHashService.VerifyPassword(password, hash1)); // Both should verify against the original password
            Assert.True(_passwordHashService.VerifyPassword(password, hash2));
        }
    }
}
