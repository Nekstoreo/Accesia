namespace Accesia.Tests.UnitTests.Domain.Entities;

public class UserTests
{
    #region Helpers para pruebas

    private static Email CreateValidEmail() => Email.Create("test@example.com");
    private static string CreateValidPasswordHash() => "hashed_password_123";
    private static string CreateValidFirstName() => "John";
    private static string CreateValidLastName() => "Doe";

    #endregion

    #region Pruebas del constructor y métodos de fábrica

    [Fact]
    public void Constructor_ConParametrosValidos_DeberiaCrearUsuarioConPropiedadesEsperadas()
    {
        // Arrange
        var email = CreateValidEmail();
        var passwordHash = CreateValidPasswordHash();
        var firstName = CreateValidFirstName();
        var lastName = CreateValidLastName();

        // Act
        var user = new User(email, passwordHash, firstName, lastName);

        // Assert
        user.Email.Should().Be(email, "El email del usuario debe coincidir con el proporcionado.");
        user.PasswordHash.Should().Be(passwordHash, "El hash de la contraseña debe coincidir con el proporcionado.");
        user.FirstName.Should().Be(firstName, "El nombre debe coincidir con el proporcionado.");
        user.LastName.Should().Be(lastName, "El apellido debe coincidir con el proporcionado.");
        user.Status.Should().Be(UserStatus.PendingConfirmation, "El estado inicial del usuario debe ser PendingConfirmation.");
        user.IsEmailVerified.Should().BeFalse("El email no debe estar verificado inicialmente.");
        user.FailedLoginAttempts.Should().Be(0, "El contador de intentos de login fallidos debe ser 0 inicialmente.");
        user.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "La fecha de cambio de contraseña debe ser cercana a la actual.");
    }

    [Fact]
    public void CreateNewUser_ConParametrosValidos_DeberiaCrearUsuarioConPropiedadesEsperadas()
    {
        // Arrange
        var email = CreateValidEmail();
        var passwordHash = CreateValidPasswordHash();
        var firstName = CreateValidFirstName();
        var lastName = CreateValidLastName();

        // Act
        var user = User.CreateNewUser(email, passwordHash, firstName, lastName);

        // Assert
        user.Email.Should().Be(email, "El email del usuario creado debe coincidir.");
        user.PasswordHash.Should().Be(passwordHash, "El hash de la contraseña del usuario creado debe coincidir.");
        user.FirstName.Should().Be(firstName, "El nombre del usuario creado debe coincidir.");
        user.LastName.Should().Be(lastName, "El apellido del usuario creado debe coincidir.");
        user.Status.Should().Be(UserStatus.PendingConfirmation, "El estado del nuevo usuario debe ser PendingConfirmation.");
        user.IsEmailVerified.Should().BeFalse("El email del nuevo usuario no debe estar verificado.");
        user.FailedLoginAttempts.Should().Be(0, "Los intentos de login fallidos del nuevo usuario deben ser 0.");
    }

    #endregion

    #region Pruebas de Verificación de Email

    [Fact]
    public void VerifyEmail_CuandoEmailNoEstaVerificado_DeberiaMarcarEmailComoVerificado()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());

        // Act
        user.VerifyEmail();

        // Assert
        user.IsEmailVerified.Should().BeTrue("El email debe ser marcado como verificado.");
        user.EmailVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "La fecha de verificación del email debe ser cercana a la actual.");
    }

    [Fact]
    public void VerifyEmail_CuandoEmailYaEstaVerificado_DeberiaLanzarInvalidOperationException()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.VerifyEmail(); // Verificar primero

        // Act & Assert
        var action = () => user.VerifyEmail();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("El email ya está verificado.", "Debe lanzar una excepción si el email ya está verificado.");
    }

    #endregion

    #region Pruebas de Bloqueo de Cuenta

    [Fact]
    public void LockAccount_ConDuracion_DeberiaEstablecerLockedUntilYEstadoBloqueado()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var lockDuration = TimeSpan.FromMinutes(30);

        // Act
        user.LockAccount(lockDuration);

        // Assert
        user.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.Add(lockDuration), TimeSpan.FromSeconds(1), "La cuenta debe estar bloqueada hasta la duración especificada.");
        user.Status.Should().Be(UserStatus.Blocked, "El estado del usuario debe cambiar a Bloqueado.");
    }

    [Fact]
    public void UnlockAccount_CuandoCuentaEstaBloqueada_DeberiaQuitarBloqueoYActivarCuenta()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.LockAccount(TimeSpan.FromMinutes(30));
        user.IncrementFailedLoginAttempts();

        // Act
        user.UnlockAccount();

        // Assert
        user.LockedUntil.Should().BeNull("LockedUntil debe ser nulo después de desbloquear la cuenta.");
        user.Status.Should().Be(UserStatus.Active, "El estado del usuario debe cambiar a Activo después de desbloquear la cuenta.");
        user.FailedLoginAttempts.Should().Be(0, "Los intentos de login fallidos deben ser reiniciados a 0.");
    }

    [Fact]
    public void IsAccountLocked_CuandoLockedUntilEsEnFuturo_DeberiaRetornarTrue()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.LockAccount(TimeSpan.FromMinutes(30));

        // Act & Assert
        user.IsAccountLocked().Should().BeTrue("La cuenta debe ser considerada bloqueada si LockedUntil está en el futuro.");
    }

    [Fact]
    public void IsAccountLocked_CuandoLockedUntilEsEnPasado_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.LockedUntil = DateTime.UtcNow.AddMinutes(-10); // Lock expirado

        // Act & Assert
        user.IsAccountLocked().Should().BeFalse("La cuenta no debe ser considerada bloqueada si LockedUntil está en el pasado.");
    }

    #endregion

    #region Pruebas de Intentos de Login Fallidos

    [Fact]
    public void IncrementFailedLoginAttempts_DeberiaAumentarContador()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());

        // Act
        user.IncrementFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(1, "El contador de intentos fallidos debe incrementarse a 1.");
    }

    [Fact]
    public void IncrementFailedLoginAttempts_AlAlcanzarMaximosIntentos_DeberiaBloquearCuenta()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());

        // Act - Incrementar hasta el límite (5 intentos)
        for (int i = 0; i < 5; i++)
        {
            user.IncrementFailedLoginAttempts();
        }

        // Assert
        user.FailedLoginAttempts.Should().Be(5, "El contador de intentos fallidos debe ser igual al máximo.");
        user.Status.Should().Be(UserStatus.Blocked, "El estado del usuario debe cambiar a Bloqueado al alcanzar el límite.");
        user.IsAccountLocked().Should().BeTrue("La cuenta debe estar bloqueada al alcanzar el límite de intentos fallidos.");
    }

    [Fact]
    public void ResetFailedLoginAttempts_DeberiaPonerContadorACero()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts();

        // Act
        user.ResetFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(0, "El contador de intentos fallidos debe ser 0 después de reiniciar.");
    }

    [Fact]
    public void OnSuccessfulLogin_DeberiaActualizarLastLoginYReiniciarIntentosFallidos()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.IncrementFailedLoginAttempts();

        // Act
        user.OnSuccessfulLogin();

        // Assert
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "LastLoginAt debe actualizarse a la hora actual.");
        user.FailedLoginAttempts.Should().Be(0, "Los intentos fallidos deben ser reiniciados a 0 tras un login exitoso.");
    }

    #endregion

    #region Pruebas de Gestión de Contraseñas

    [Fact]
    public void ChangePassword_ConHashValido_DeberiaActualizarContraseñaYTimestamp()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var newPasswordHash = "new_hashed_password_456";

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash, "El hash de la contraseña debe actualizarse a la nueva.");
        user.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "La fecha de cambio de contraseña debe actualizarse.");
        user.PasswordResetToken.Should().BeNull("El token de reseteo de contraseña debe ser nulo después del cambio.");
        user.PasswordResetTokenExpiresAt.Should().BeNull("La fecha de expiración del token de reseteo debe ser nula después del cambio.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePassword_ConHashInvalido_DeberiaLanzarArgumentException(string invalidHash)
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());

        // Act & Assert
        var action = () => user.ChangePassword(invalidHash);
        action.Should().Throw<ArgumentException>()
            .WithMessage("El hash de la contraseña no puede ser nulo, vacío o espacios en blanco.", "Debe lanzar una excepción para un hash de contraseña inválido.");
    }

    [Fact]
    public void SetPasswordResetToken_ConParametrosValidos_DeberiaEstablecerTokenYExpiracion()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var token = "resetToken123";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        user.SetPasswordResetToken(token, expiresAt);

        // Assert
        user.PasswordResetToken.Should().Be(token, "El token de reseteo de contraseña debe ser establecido.");
        user.PasswordResetTokenExpiresAt.Should().Be(expiresAt, "La fecha de expiración del token de reseteo debe ser establecida.");
    }

    [Fact]
    public void SetEmailVerificationToken_ConParametrosValidos_DeberiaEstablecerTokenYExpiracion()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var token = "verifyToken123";
        var expiresAt = DateTime.UtcNow.AddHours(2);

        // Act
        user.SetEmailVerificationToken(token, expiresAt);

        // Assert
        user.EmailVerificationToken.Should().Be(token, "El token de verificación de email debe ser establecido.");
        user.EmailVerificationTokenExpiresAt.Should().Be(expiresAt, "La fecha de expiración del token de verificación de email debe ser establecida.");
    }

    [Fact]
    public void IsPasswordResetTokenValid_ConTokenValido_DeberiaRetornarTrue()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.SetPasswordResetToken("token", DateTime.UtcNow.AddHours(1));

        // Act & Assert
        user.IsPasswordResetTokenValid("token").Should().BeTrue("El token de reseteo de contraseña debe ser válido.");
    }

    [Fact]
    public void IsPasswordResetTokenValid_ConTokenExpirado_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.SetPasswordResetToken("token", DateTime.UtcNow.AddMinutes(-10)); // Expirado

        // Act & Assert
        user.IsPasswordResetTokenValid("token").Should().BeFalse("El token de reseteo de contraseña debe ser inválido si ha expirado.");
    }

    [Fact]
    public void IsPasswordResetTokenValid_ConTokenIncorrecto_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.SetPasswordResetToken("correctToken", DateTime.UtcNow.AddHours(1));

        // Act & Assert
        user.IsPasswordResetTokenValid("incorrectToken").Should().BeFalse("El token de reseteo de contraseña debe ser inválido si no coincide.");
    }

    #endregion

    #region Pruebas de Gestión de Perfil

    [Fact]
    public void UpdateProfile_ConParametrosValidos_DeberiaActualizarTodosLosCampos()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), "Old", "Name");
        var newFirstName = "New";
        var newLastName = "User";
        var newEmail = Email.Create("new@example.com");

        // Act
        user.UpdateProfile(newFirstName, newLastName, newEmail);

        // Assert
        user.FirstName.Should().Be(newFirstName, "El nombre debe actualizarse.");
        user.LastName.Should().Be(newLastName, "El apellido debe actualizarse.");
        user.Email.Should().Be(newEmail, "El email debe actualizarse.");
        user.EmailVerifiedAt.Should().BeNull("EmailVerifiedAt debe ser nulo después de un cambio de email.");
        user.IsEmailVerified.Should().BeFalse("IsEmailVerified debe ser falso después de un cambio de email.");
    }

    [Fact]
    public void UpdateProfile_ConParametrosNulos_NoDeberiaActualizarCampos()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), "John", "Doe");
        var originalFirstName = user.FirstName;
        var originalLastName = user.LastName;
        var originalEmail = user.Email;

        // Act
        user.UpdateProfile(null, null, null);

        // Assert
        user.FirstName.Should().Be(originalFirstName, "El nombre no debe cambiar si es nulo.");
        user.LastName.Should().Be(originalLastName, "El apellido no debe cambiar si es nulo.");
        user.Email.Should().Be(originalEmail, "El email no debe cambiar si es nulo.");
        user.EmailVerifiedAt.Should().Be(null, "EmailVerifiedAt no debe cambiar si el email es nulo.");
        user.IsEmailVerified.Should().BeFalse("IsEmailVerified no debe cambiar si el email es nulo.");
    }

    #endregion

    #region Pruebas de Eliminación de Cuenta

    [Fact]
    public void RequestAccountDeletion_ConParametrosValidos_DeberiaEstablecerTokenDeEliminacion()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var token = "deletionToken123";
        var gracePeriod = TimeSpan.FromDays(7);

        // Act
        user.RequestAccountDeletion(token, gracePeriod);

        // Assert
        user.AccountDeletionToken.Should().Be(token, "El token de eliminación de cuenta debe ser establecido.");
        user.AccountDeletionRequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "AccountDeletionRequestedAt debe ser cercana a la actual.");
        user.AccountDeletionGracePeriodEnd.Should().BeCloseTo(DateTime.UtcNow.Add(gracePeriod), TimeSpan.FromSeconds(1), "AccountDeletionGracePeriodEnd debe ser establecida correctamente.");
        user.Status.Should().Be(UserStatus.MarkedForDeletion, "El estado del usuario debe cambiar a MarkedForDeletion.");
    }

    [Fact]
    public void ConfirmAccountDeletion_ConTokenValido_DeberiaCambiarEstadoAMarcadoParaEliminacion()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.RequestAccountDeletion("validToken", TimeSpan.FromDays(7));

        // Act
        user.ConfirmAccountDeletion("validToken");

        // Assert
        user.Status.Should().Be(UserStatus.MarkedForDeletion, "El estado debe ser MarkedForDeletion después de confirmar la eliminación.");
        user.AccountDeletionToken.Should().BeNull("El token de eliminación debe ser nulo después de confirmar.");
    }

    [Fact]
    public void CancelAccountDeletion_CuandoEstaEnProcesoDeEliminacion_DeberiaLimpiarCamposDeEliminacion()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.RequestAccountDeletion("token", TimeSpan.FromDays(7));

        // Act
        user.CancelAccountDeletion();

        // Assert
        user.AccountDeletionToken.Should().BeNull("AccountDeletionToken debe ser nulo.");
        user.AccountDeletionRequestedAt.Should().BeNull("AccountDeletionRequestedAt debe ser nulo.");
        user.AccountDeletionGracePeriodEnd.Should().BeNull("AccountDeletionGracePeriodEnd debe ser nulo.");
        user.Status.Should().Be(UserStatus.Active, "El estado debe volver a Activo.");
    }

    [Fact]
    public void IsAccountDeletionTokenValid_ConTokenValido_DeberiaRetornarTrue()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.RequestAccountDeletion("testToken", TimeSpan.FromDays(1));

        // Act & Assert
        user.IsAccountDeletionTokenValid("testToken").Should().BeTrue("El token de eliminación de cuenta debe ser válido.");
    }

    [Fact]
    public void IsInGracePeriod_CuandoEstaDentroDePeriodoDeGracia_DeberiaRetornarTrue()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.RequestAccountDeletion("token", TimeSpan.FromDays(7));

        // Act & Assert
        user.IsInGracePeriod().Should().BeTrue("El usuario debe estar en el período de gracia.");
    }

    [Fact]
    public void IsInGracePeriod_CuandoEstaFueraDePeriodoDeGracia_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.AccountDeletionRequestedAt = DateTime.UtcNow.AddDays(-10); // Solicitud en el pasado
        user.AccountDeletionGracePeriodEnd = DateTime.UtcNow.AddDays(-3); // Período de gracia terminado

        // Act & Assert
        user.IsInGracePeriod().Should().BeFalse("El usuario no debe estar en el período de gracia si ha terminado.");
    }

    #endregion

    #region Pruebas de Lógica de Login y Estado de la Cuenta

    [Fact]
    public void CanAttemptLogin_ConUsuarioActivoYNoBloqueado_DeberiaRetornarTrue()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.ActivateAccount(); // Asegurar que está activo

        // Act & Assert
        user.CanAttemptLogin().Should().BeTrue("Un usuario activo y no bloqueado debe poder intentar iniciar sesión.");
    }

    [Fact]
    public void CanAttemptLogin_ConCuentaBloqueada_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.LockAccount(TimeSpan.FromMinutes(30));

        // Act & Assert
        user.CanAttemptLogin().Should().BeFalse("Un usuario bloqueado no debe poder intentar iniciar sesión.");
    }

    [Fact]
    public void CanAttemptLogin_ConDemasiadosIntentosFallidos_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        for (int i = 0; i < 5; i++) user.IncrementFailedLoginAttempts(); // Bloquear por intentos fallidos

        // Act & Assert
        user.CanAttemptLogin().Should().BeFalse("Un usuario con demasiados intentos fallidos no debe poder intentar iniciar sesión.");
    }

    [Fact]
    public void CanLogin_ConEstadoInactivo_DeberiaRetornarFalse()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.DeactivateAccount();

        // Act & Assert
        user.CanLogin().Should().BeFalse("Un usuario inactivo no debe poder iniciar sesión.");
    }

    [Fact]
    public void ActivateAccount_DeberiaCambiarEstadoAActivo()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());

        // Act
        user.ActivateAccount();

        // Assert
        user.Status.Should().Be(UserStatus.Active, "El estado del usuario debe cambiar a Activo.");
    }

    [Fact]
    public void DeactivateAccount_DeberiaCambiarEstadoAInactivo()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.ActivateAccount();

        // Act
        user.DeactivateAccount();

        // Assert
        user.Status.Should().Be(UserStatus.Inactive, "El estado del usuario debe cambiar a Inactivo.");
    }

    [Fact]
    public void BlockAccount_ConRazon_DeberiaCambiarEstadoABloqueado()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var reason = "Violación de políticas";

        // Act
        user.BlockAccount(reason);

        // Assert
        user.Status.Should().Be(UserStatus.Blocked, "El estado del usuario debe cambiar a Bloqueado.");
        user.BlockedReason.Should().Be(reason, "La razón de bloqueo debe ser establecida.");
    }

    [Theory]
    [InlineData(UserStatus.PendingConfirmation, "Pendiente de confirmación de email")]
    [InlineData(UserStatus.Active, "Cuenta activa")]
    [InlineData(UserStatus.Inactive, "Cuenta inactiva - requiere reactivación")]
    [InlineData(UserStatus.Blocked, "Cuenta bloqueada")]
    [InlineData(UserStatus.Pending, "Estado desconocido")]
    [InlineData(UserStatus.EmailPendingVerification, "Verificación de email pendiente")]
    [InlineData(UserStatus.MarkedForDeletion, "Marcada para eliminación")]
    public void GetStatusDescription_ParaCadaEstado_DeberiaRetornarDescripcionCorrecta(UserStatus status, string expectedDescription)
    {
        // Arrange
        var user = new User(CreateValidEmail(), CreateValidPasswordHash(), "", "") { Status = status };

        // Act
        var description = user.GetStatusDescription();

        // Assert
        description.Should().Be(expectedDescription, $"La descripción del estado {status} debe ser '{expectedDescription}'.");
    }

    #endregion

    #region Pruebas de Gestión de Configuración de Usuario (UserSettings)

    [Fact]
    public void EnsureSettingsExist_CuandoSettingsEsNulo_DeberiaCrearNuevasSettings()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.UserSettings = null; // Asegurar que sea nulo inicialmente

        // Act
        user.EnsureSettingsExist();

        // Assert
        user.UserSettings.Should().NotBeNull("UserSettings debe ser creado si es nulo.");
        user.UserSettings.UserId.Should().Be(user.Id, "El UserId de UserSettings debe coincidir con el del usuario.");
    }

    [Fact]
    public void EnsureSettingsExist_CuandoSettingsExisten_NoDeberiaCrearNuevas()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        var existingSettings = new UserSettings(user.Id);
        user.UserSettings = existingSettings;

        // Act
        user.EnsureSettingsExist();

        // Assert
        user.UserSettings.Should().Be(existingSettings, "UserSettings no debe ser recreado si ya existe.");
    }

    [Fact]
    public void GetSettings_CuandoSettingsEsNulo_DeberiaCrearYRetornarNuevasSettings()
    {
        // Arrange
        var user = User.CreateNewUser(CreateValidEmail(), CreateValidPasswordHash(), CreateValidFirstName(), CreateValidLastName());
        user.UserSettings = null; // Asegurar que sea nulo inicialmente

        // Act
        var settings = user.GetSettings();

        // Assert
        settings.Should().NotBeNull("GetSettings debe crear y retornar UserSettings si es nulo.");
        user.UserSettings.Should().Be(settings, "La propiedad UserSettings del usuario debe ser la misma instancia retornada.");
    }

    #endregion
}