using Xunit;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync, PostAsJsonAsync
using System.Threading.Tasks;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Domain.Entities; // For User, Session
using Accesia.Domain.Enums;   // For UserStatus, SessionStatus
using Accesia.Infrastructure.Data; // For ApplicationDbContext
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Moq; // For verifying EmailServiceMock
using System;
using System.Collections.Generic; // For List
using Microsoft.EntityFrameworkCore; // For FirstOrDefaultAsync, etc.

// Determine the correct Program class based on Accesia.API's structure.
// If Accesia.API/Program.cs has "public partial class Program { }", then use 'Program'.
// If it has "namespace Accesia.API; public partial class Program { }", then use 'Accesia.API.Program'.
// For this example, I'll assume 'Program' refers to the entry point class of Accesia.API.
// You might need to add: using Accesia.API; if Program is namespaced.
// Or use global::Program if it's in the global namespace of Accesia.API project.
// Let's assume Program is accessible. For top-level statements in .NET 6+,
// ensure `public partial class Program { }` is added to Program.cs and
// `<InternalsVisibleTo Include="Accesia.API.IntegrationTests" />` to Accesia.API.csproj.
// Using 'Program' which should resolve to Accesia.API.Program if correctly configured.

namespace Accesia.API.IntegrationTests
{
    /// <summary>
    /// Pruebas de integración para <see cref="Accesia.API.Controllers.AuthController"/>.
    /// Estas pruebas verifican los flujos de autenticación completos interactuando directamente
    /// con los endpoints HTTP de la API. Utiliza una instancia de <see cref="CustomWebApplicationFactory{TProgram}"/>
    /// para configurar un entorno de prueba autocontenido con una base de datos en memoria y mocks
    /// para servicios externos como IEmailService.
    /// Cada prueba busca simular el comportamiento de un cliente real de la API.
    /// </summary>
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime // Changed TProgram to Program
    {
        private readonly CustomWebApplicationFactory<Program> _factory; // Changed TProgram to Program
        private readonly HttpClient _client;
        private readonly Mock<IEmailService> _emailServiceMock;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory) // Changed TProgram to Program
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _emailServiceMock = _factory.EmailServiceMock; // Get the mock from the factory
        }

        public async Task InitializeAsync()
        {
            // Reset mocks and database before each test execution
            _factory.ResetMocks();
            await ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            // Cleanup after tests if needed, though factory re-creation or DB per test handles isolation
            return Task.CompletedTask;
        }

        private async Task ResetDatabaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // This is for InMemory. For a real DB, you'd use Respawn or similar.
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }


        private async Task<(User User, string PlainPassword, string VerificationToken)> RegisterUserAndGetVerificationToken(string email, string password, string firstName = "Test", string lastName = "User")
        {
            var registerRequest = new RegisterUserRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                FirstName = firstName,
                LastName = lastName
            };

            string capturedToken = null;
            _emailServiceMock
                .Setup(s => s.SendEmailVerificationAsync(email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((e, t, ct) => capturedToken = t)
                .Returns(Task.CompletedTask);

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            response.EnsureSuccessStatusCode(); // Throws if not 2xx

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
            Assert.NotNull(user);
            // The token is now captured by the mock callback. If not, get from user.EmailVerificationToken.
            Assert.NotNull(capturedToken);

            return (user, password, capturedToken ?? user.EmailVerificationToken);
        }


        [Fact]
        public async Task FullAuthenticationFlow_HappyPath_ShouldSucceed()
        {
            var userEmail = $"testuser_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
            var userPassword = "Password123!";

            // 1. Register User
            string verificationToken = null;
            Guid userId = Guid.Empty;

            _emailServiceMock
                .Setup(s => s.SendEmailVerificationAsync(userEmail, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((email, token, ct) => verificationToken = token)
                .Returns(Task.CompletedTask);

            var registerRequest = new RegisterUserRequest
            {
                Email = userEmail,
                Password = userPassword,
                ConfirmPassword = userPassword,
                FirstName = "Happy",
                LastName = "Path"
            };
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();
            Assert.NotNull(registerResult);
            Assert.True(registerResult.Success);
            Assert.True(registerResult.RequiresEmailVerification);
            userId = registerResult.UserId;

            _emailServiceMock.Verify(s => s.SendEmailVerificationAsync(userEmail, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(verificationToken); // Token should have been captured

            // Check DB for user state after registration
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbUser = await dbContext.Users.FindAsync(userId);
                Assert.NotNull(dbUser);
                Assert.Equal(UserStatus.PendingConfirmation, dbUser.Status);
                Assert.False(dbUser.IsEmailVerified);
                Assert.Equal(verificationToken, dbUser.EmailVerificationToken);
            }

            // 2. Verify Email
            var verifyRequest = new VerifyEmailRequest { Token = verificationToken, Email = userEmail };
            var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);
            Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
            var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<VerifyEmailResponse>();
            Assert.NotNull(verifyResult);
            Assert.True(verifyResult.Success);
            Assert.True(verifyResult.IsAccountActivated);

            // Check DB for user state after verification
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbUser = await dbContext.Users.FindAsync(userId);
                Assert.NotNull(dbUser);
                Assert.Equal(UserStatus.Active, dbUser.Status);
                Assert.True(dbUser.IsEmailVerified);
                Assert.NotNull(dbUser.EmailVerifiedAt);
                Assert.Null(dbUser.EmailVerificationToken);
            }

            // 3. Login User
            var loginRequest = new LoginRequest { Email = userEmail, Password = userPassword, DeviceName = "Test Rig" };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(loginResult);
            Assert.NotEmpty(loginResult.AccessToken);
            Assert.NotEmpty(loginResult.RefreshToken);
            Assert.NotNull(loginResult.User);
            Assert.Equal(userId, loginResult.User.Id);
            Assert.NotNull(loginResult.Session);
            Assert.NotEmpty(loginResult.Session.SessionId); // This is the SessionToken

            // Check DB for session state
            string sessionTokenForLogout = loginResult.Session.SessionId;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbSession = await dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionToken == sessionTokenForLogout);
                Assert.NotNull(dbSession);
                Assert.Equal(userId, dbSession.UserId);
                Assert.Equal(SessionStatus.Active, dbSession.Status);
            }

            // 4. Logout User
            var logoutRequest = new LogoutRequest { SessionToken = sessionTokenForLogout };
            var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
            var logoutResult = await logoutResponse.Content.ReadFromJsonAsync<LogoutResponse>();
            Assert.NotNull(logoutResult);
            Assert.True(logoutResult.Success);

            // Check DB for session state after logout
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbSession = await dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionToken == sessionTokenForLogout);
                Assert.NotNull(dbSession);
                Assert.Equal(SessionStatus.Revoked, dbSession.Status); // SessionService marks as Revoked
            }
        }

        [Fact]
        public async Task Register_EmailAlreadyExists_ShouldReturnConflict()
        {
            // Arrange
            var email = $"conflict_{Guid.NewGuid().ToString().Substring(0,8)}@example.com";
            await RegisterUserAndGetVerificationToken(email, "Password123!"); // First registration

            var conflictingRequest = new RegisterUserRequest
            {
                Email = email, // Same email
                Password = "OtherPassword1!",
                ConfirmPassword = "OtherPassword1!",
                FirstName = "Conflict",
                LastName = "User"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", conflictingRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<object>(); // Or a specific error DTO
            Assert.NotNull(error);
            // Optionally, assert error message content if it's standardized
        }


        [Fact]
        public async Task Login_UserNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "nonexistent@example.com", Password = "password" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Login_IncorrectPassword_ShouldReturnUnauthorizedAndIncrementFailedAttempts()
        {
            // Arrange
            var email = $"loginfail_{Guid.NewGuid().ToString().Substring(0,8)}@example.com";
            var (user, plainPassword, verificationToken) = await RegisterUserAndGetVerificationToken(email, "CorrectPassword123!");

            // Verify email to activate account
            var verifyReq = new VerifyEmailRequest { Token = verificationToken, Email = email };
            await _client.PostAsJsonAsync("/api/auth/verify-email", verifyReq); // Ensure OK

            var loginRequest = new LoginRequest { Email = email, Password = "IncorrectPassword!" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // AuthController returns 401 for InvalidCredentialsException

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbUser = await dbContext.Users.FindAsync(user.Id);
            Assert.NotNull(dbUser);
            Assert.Equal(1, dbUser.FailedLoginAttempts);
        }

        [Fact]
        public async Task Login_AccountLocked_ShouldReturnLocked()
        {
            // Arrange
            var email = $"locktest_{Guid.NewGuid().ToString().Substring(0,8)}@example.com";
            var (user, _, verificationToken) = await RegisterUserAndGetVerificationToken(email, "CorrectPassword123!");

            var verifyReq = new VerifyEmailRequest { Token = verificationToken, Email = email };
            await _client.PostAsJsonAsync("/api/auth/verify-email", verifyReq); // Activate

            // Simulate 5 failed login attempts
            for (int i = 0; i < 5; i++)
            {
                var failLoginReq = new LoginRequest { Email = email, Password = $"WrongPassword{i}!" };
                await _client.PostAsJsonAsync("/api/auth/login", failLoginReq);
                // We expect Unauthorized for these attempts until locked.
            }

            // The 6th attempt should find the account locked.
            var finalLoginReq = new LoginRequest { Email = email, Password = "CorrectPassword123!" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", finalLoginReq);

            // Assert
            Assert.Equal(HttpStatusCode.Locked, response.StatusCode); // 423 Locked
            Assert.True(response.Headers.Contains("Retry-After"));

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbUser = await dbContext.Users.FindAsync(user.Id);
            Assert.NotNull(dbUser);
            Assert.Equal(UserStatus.Blocked, dbUser.Status);
            Assert.True(dbUser.IsAccountLocked());
        }


        [Fact]
        public async Task VerifyEmail_InvalidToken_ShouldReturnNotFound()
        {
            // Arrange
            var request = new VerifyEmailRequest { Token = "this_is_an_invalid_token", Email = "any@example.com" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-email", request);

            // Assert
            // Based on AuthController, InvalidVerificationTokenException results in 404.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ValidRefreshToken_ShouldReturnNewTokens()
        {
            // Arrange
            var email = $"refresh_{Guid.NewGuid().ToString().Substring(0,8)}@example.com";
            var (user, password, verificationToken) = await RegisterUserAndGetVerificationToken(email, "PasswordForRefresh!");
            await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest { Token = verificationToken, Email = email });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
            var initialLoginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(initialLoginResult?.RefreshToken);

            var refreshTokenRequest = new RefreshTokenRequest { RefreshToken = initialLoginResult.RefreshToken };

            // Act
            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshTokenRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            Assert.NotNull(refreshResult);
            Assert.NotEmpty(refreshResult.AccessToken);
            Assert.NotEmpty(refreshResult.RefreshToken);
            Assert.NotEqual(initialLoginResult.AccessToken, refreshResult.AccessToken);
            Assert.NotEqual(initialLoginResult.RefreshToken, refreshResult.RefreshToken); // Should be a new refresh token
        }

        [Fact]
        public async Task LogoutAllDevices_ValidCurrentSession_ShouldRevokeAllUserSessions()
        {
            // Arrange
            var email = $"logoutall_{Guid.NewGuid().ToString().Substring(0,8)}@example.com";
            var password = "Password123!";
            var (user, _, verificationToken) = await RegisterUserAndGetVerificationToken(email, password);
            await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest { Token = verificationToken, Email = email });

            // Session 1
            var login1Response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password, DeviceName = "Device1" });
            var login1Result = await login1Response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(login1Result?.Session?.SessionId);

            // Session 2
            var login2Response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password, DeviceName = "Device2" });
            var login2Result = await login2Response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(login2Result?.Session?.SessionId);

            var logoutAllRequest = new LogoutAllDevicesRequest { CurrentSessionToken = login1Result.Session.SessionId };

            // Act
            var logoutAllResponse = await _client.PostAsJsonAsync("/api/auth/logout-all-devices", logoutAllRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, logoutAllResponse.StatusCode);
            var logoutAllResult = await logoutAllResponse.Content.ReadFromJsonAsync<LogoutAllDevicesResponse>();
            Assert.NotNull(logoutAllResult);
            Assert.True(logoutAllResult.Success);
            // The handler for LogoutAllDevices revokes ALL sessions for the user.
            // The count should be the total number of active sessions before this call.
            Assert.Equal(2, logoutAllResult.SessionsTerminated);


            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sessions = await dbContext.Sessions.Where(s => s.UserId == user.Id).ToListAsync();
            Assert.True(sessions.All(s => s.Status == SessionStatus.Revoked));
        }
    }
}
