using Xunit;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System;
using System.Linq; // For permission checks
using System.Collections.Generic;


namespace Accesia.Domain.Tests.Entities
{
    /// <summary>
    /// Pruebas unitarias para la entidad <see cref="User"/>.
    /// Estas pruebas se centran en la lógica de negocio intrínseca de la entidad User,
    /// como las transiciones de estado (bloqueo, verificación), manejo de intentos de login,
    /// y la correcta inicialización y modificación de sus propiedades.
    /// No se utilizan mocks ya que se prueba la lógica interna de la clase.
    /// </summary>
    public class UserTests
    {
        private User CreateTestUser(
            string email = "test@example.com",
            string passwordHash = "hashed_password",
            string firstName = "Test",
            string lastName = "User")
        {
            // Using the static factory method as intended
            return User.CreateNewUser(new Email(email), passwordHash, firstName, lastName);
        }

        [Fact]
        public void CreateNewUser_ValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var email = new Email("new@example.com");
            var passwordHash = "new_hash";
            var firstName = "New";
            var lastName = "User";

            // Act
            var user = User.CreateNewUser(email, passwordHash, firstName, lastName);

            // Assert
            Assert.Equal(email, user.Email);
            Assert.Equal(passwordHash, user.PasswordHash);
            Assert.Equal(firstName, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Equal(UserStatus.PendingConfirmation, user.Status);
            Assert.False(user.IsEmailVerified);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            Assert.Null(user.LastLoginAt);
            Assert.NotNull(user.PasswordChangedAt); // Should be set on creation
            Assert.Equal("es", user.PreferredLanguage); // Default value
            Assert.Equal("America/Bogota", user.TimeZone); // Default value
        }

        [Fact]
        public void VerifyEmail_WhenNotVerified_SetsIsEmailVerifiedAndDateAndStatusToActiveIfPending()
        {
            // Arrange
            var user = CreateTestUser();
            user.Status = UserStatus.PendingConfirmation; // Ensure correct initial state
            Assert.False(user.IsEmailVerified);
            Assert.Null(user.EmailVerifiedAt);

            // Act
            user.VerifyEmail(); // This method is on User entity, but VerifyEmailHandler changes status
                                // The User.VerifyEmail only sets IsEmailVerified and EmailVerifiedAt.
                                // The handler then sets status. For this unit test, we test User.VerifyEmail only.

            // Assert
            Assert.True(user.IsEmailVerified);
            Assert.NotNull(user.EmailVerifiedAt);
            Assert.True(user.EmailVerifiedAt <= DateTime.UtcNow && user.EmailVerifiedAt > DateTime.UtcNow.AddSeconds(-5));
            // Status change to Active is typically handled by the Application layer (VerifyEmailHandler) after calling user.VerifyEmail()
            // So, here we only check the direct effects of User.VerifyEmail()
            // If User.VerifyEmail() itself was supposed to change status, that would be asserted here.
            // Based on VerifyEmailHandler, User.VerifyEmail() does not change status directly.
            // Let's re-check User.VerifyEmail() implementation if it should change status.
            // User.VerifyEmail() only sets IsEmailVerified and EmailVerifiedAt.
        }


        [Fact]
        public void VerifyEmail_WhenAlreadyVerified_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = CreateTestUser();
            user.VerifyEmail(); // Verify once
            user.Status = UserStatus.Active; // Simulate handler having set it

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => user.VerifyEmail());
            Assert.Equal("El email ya está verificado.", exception.Message);
        }

        [Fact]
        public void LockAccount_SetsLockedUntilAndStatusToBlocked()
        {
            // Arrange
            var user = CreateTestUser();
            user.Status = UserStatus.Active; // Initial state
            var lockDuration = TimeSpan.FromHours(1);

            // Act
            user.LockAccount(lockDuration);

            // Assert
            Assert.Equal(UserStatus.Blocked, user.Status);
            Assert.NotNull(user.LockedUntil);
            Assert.True(user.LockedUntil > DateTime.UtcNow);
            Assert.True(user.LockedUntil <= DateTime.UtcNow.Add(lockDuration).AddSeconds(1)); // Allow slight diff
        }

        [Fact]
        public void UnlockAccount_WhenLocked_ClearsLockedUntilAndResetsFailedAttemptsAndSetsStatusToActive()
        {
            // Arrange
            var user = CreateTestUser();
            user.LockAccount(TimeSpan.FromHours(1));
            user.FailedLoginAttempts = 3; // Simulate some failed attempts
            Assert.Equal(UserStatus.Blocked, user.Status);

            // Act
            user.UnlockAccount();

            // Assert
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.Null(user.LockedUntil);
            Assert.Equal(0, user.FailedLoginAttempts);
        }

        [Fact]
        public void IncrementFailedLoginAttempts_BelowThreshold_IncrementsCounter()
        {
            // Arrange
            var user = CreateTestUser(); // Max attempts is 5 internally
            user.Status = UserStatus.Active;
            Assert.Equal(0, user.FailedLoginAttempts);

            // Act
            user.IncrementFailedLoginAttempts(); // 1
            user.IncrementFailedLoginAttempts(); // 2

            // Assert
            Assert.Equal(2, user.FailedLoginAttempts);
            Assert.Equal(UserStatus.Active, user.Status); // Should not be locked yet
            Assert.False(user.IsAccountLocked());
        }

        [Fact]
        public void IncrementFailedLoginAttempts_ReachingThreshold_LocksAccountAndSetsStatusToBlocked()
        {
            // Arrange
            var user = CreateTestUser(); // Max attempts is 5
            user.Status = UserStatus.Active;
            user.FailedLoginAttempts = 4; // One away from lock

            // Act
            user.IncrementFailedLoginAttempts(); // This should be the 5th attempt, triggering lock

            // Assert
            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.Equal(UserStatus.Blocked, user.Status);
            Assert.True(user.IsAccountLocked());
            Assert.NotNull(user.LockedUntil);
            // The first lockout duration is 2 minutes (2^(5-5+1) = 2^1 = 2)
            Assert.True(user.LockedUntil <= DateTime.UtcNow.AddMinutes(2).AddSeconds(5) && user.LockedUntil > DateTime.UtcNow.AddMinutes(2).AddSeconds(-5));
        }

        [Fact]
        public void IncrementFailedLoginAttempts_ExceedingThreshold_IncreasesLockoutDuration()
        {
            // Arrange
            var user = CreateTestUser();
            user.Status = UserStatus.Active;

            // Trigger initial lock (5 attempts) -> locks for 2 mins
            for(int i=0; i<5; i++) user.IncrementFailedLoginAttempts();
            Assert.True(user.IsAccountLocked());
            var firstLockoutEnd = user.LockedUntil.Value;

            // Simulate time passing, account unlocks, but then fails again
            user.LockedUntil = DateTime.UtcNow.AddMinutes(-1); // "Unlock" by time passing
            user.Status = UserStatus.Active; // Manually set active for test simplicity
            // User.ResetFailedLoginAttempts(); // This would normally happen on successful login
            // For this test, we want to see exponential backoff, so FailedLoginAttempts remains high

            // Act: 6th failed attempt
            user.IncrementFailedLoginAttempts();

            // Assert
            Assert.Equal(6, user.FailedLoginAttempts);
            Assert.True(user.IsAccountLocked());
            // Lockout for 6th attempt: 2^(6-5+1) = 2^2 = 4 minutes
            Assert.True(user.LockedUntil.Value > firstLockoutEnd);
            Assert.True(user.LockedUntil <= DateTime.UtcNow.AddMinutes(4).AddSeconds(5) && user.LockedUntil > DateTime.UtcNow.AddMinutes(4).AddSeconds(-5));
        }


        [Fact]
        public void OnSuccessfulLogin_ResetsFailedAttemptsAndSetsLastLoginDateAndStatusToActiveIfBlockedButUnlocked()
        {
            // Arrange
            var user = CreateTestUser();
            user.FailedLoginAttempts = 2;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);
            user.Status = UserStatus.Blocked; // Simulate it was blocked
            user.LockedUntil = DateTime.UtcNow.AddMinutes(-5); // But the lock duration has passed

            // Act
            user.OnSuccessfulLogin();

            // Assert
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.NotNull(user.LastLoginAt);
            Assert.True(user.LastLoginAt <= DateTime.UtcNow && user.LastLoginAt > DateTime.UtcNow.AddSeconds(-5));
            Assert.Equal(UserStatus.Active, user.Status); // Should become active
        }

        [Fact]
        public void SetPasswordResetToken_SetsTokenAndExpiry()
        {
            // Arrange
            var user = CreateTestUser();
            var token = "reset_token_123";
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Act
            user.SetPasswordResetToken(token, expiresAt);

            // Assert
            Assert.Equal(token, user.PasswordResetToken);
            Assert.Equal(expiresAt, user.PasswordResetTokenExpiresAt);
        }

        [Fact]
        public void SetEmailVerificationToken_SetsTokenAndExpiry()
        {
            // Arrange
            var user = CreateTestUser();
            var token = "email_verify_abc";
            var expiresAt = DateTime.UtcNow.AddDays(1);

            // Act
            user.SetEmailVerificationToken(token, expiresAt);

            // Assert
            Assert.Equal(token, user.EmailVerificationToken);
            Assert.Equal(expiresAt, user.EmailVerificationTokenExpiresAt);
        }


        [Fact]
        public void HasPermission_UserWithRoleAndPermission_ReturnsTrue()
        {
            // Arrange
            var user = CreateTestUser();
            var permission = new Permission { Name = "read:data", Id = Guid.NewGuid() };
            var role = new Role { Name = "DataReader", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission> { new RolePermission { Permission = permission, PermissionId = permission.Id } } };
            user.UserRoles = new List<UserRole> { new UserRole { Role = role, RoleId = role.Id, UserId = user.Id, IsActive = true } };

            // Act
            var hasPerm = user.HasPermission("read:data");

            // Assert
            Assert.True(hasPerm);
        }

        [Fact]
        public void HasPermission_UserWithoutRoleOrPermission_ReturnsFalse()
        {
            // Arrange
            var user = CreateTestUser(); // No roles assigned by default in this test setup

            // Act
            var hasPerm = user.HasPermission("read:data");

            // Assert
            Assert.False(hasPerm);
        }

        [Fact]
        public void HasPermission_UserWithInactiveRole_ReturnsFalse()
        {
            // Arrange
            var user = CreateTestUser();
            var permission = new Permission { Name = "admin:access", Id = Guid.NewGuid() };
            var role = new Role { Name = "Admin", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission> { new RolePermission { Permission = permission, PermissionId = permission.Id } } };
            user.UserRoles = new List<UserRole> { new UserRole { Role = role, RoleId = role.Id, UserId = user.Id, IsActive = false } }; // Role is inactive

            // Act
            var hasPerm = user.HasPermission("admin:access");

            // Assert
            Assert.False(hasPerm);
        }

        [Fact]
        public void GetEffectivePermissions_UserWithMultipleRoles_ReturnsDistinctPermissions()
        {
            // Arrange
            var user = CreateTestUser();
            var perm1 = new Permission { Name = "perm1", Id = Guid.NewGuid() };
            var perm2 = new Permission { Name = "perm2", Id = Guid.NewGuid() };
            var perm3 = new Permission { Name = "perm3", Id = Guid.NewGuid() };

            var role1 = new Role { Name = "RoleA", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission> { new RolePermission { Permission = perm1 }, new RolePermission { Permission = perm2 } } };
            var role2 = new Role { Name = "RoleB", Id = Guid.NewGuid(), RolePermissions = new List<RolePermission> { new RolePermission { Permission = perm2 }, new RolePermission { Permission = perm3 } } };

            user.UserRoles = new List<UserRole>
            {
                new UserRole { Role = role1, IsActive = true },
                new UserRole { Role = role2, IsActive = true }
            };

            // Act
            var effectivePermissions = user.GetEffectivePermissions().ToList();

            // Assert
            Assert.Equal(3, effectivePermissions.Count); // perm1, perm2, perm3 (perm2 is common but distinct)
            Assert.Contains(perm1, effectivePermissions);
            Assert.Contains(perm2, effectivePermissions);
            Assert.Contains(perm3, effectivePermissions);
        }

        [Fact]
        public void HasRole_UserWithActiveRole_ReturnsTrue()
        {
            // Arrange
            var user = CreateTestUser();
            var role = new Role { Name = "Manager", Id = Guid.NewGuid() };
            user.UserRoles = new List<UserRole> { new UserRole { Role = role, IsActive = true } };

            // Act
            var hasRole = user.HasRole("Manager");
            var hasRoleLower = user.HasRole("manager");


            // Assert
            Assert.True(hasRole);
            Assert.True(hasRoleLower); // Check case-insensitivity
        }

        [Fact]
        public void HasRole_UserWithoutRoleOrInactiveRole_ReturnsFalse()
        {
            // Arrange
            var user = CreateTestUser();
            var role = new Role { Name = "Auditor", Id = Guid.NewGuid() };
            user.UserRoles = new List<UserRole> { new UserRole { Role = role, IsActive = false } };


            // Act
            var hasActiveRole = user.HasRole("Auditor");
            var hasNonExistentRole = user.HasRole("NonExistent");


            // Assert
            Assert.False(hasActiveRole); // Role is inactive
            Assert.False(hasNonExistentRole);
        }
    }
}
