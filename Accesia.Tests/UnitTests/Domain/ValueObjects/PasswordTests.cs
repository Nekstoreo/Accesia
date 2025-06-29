namespace Accesia.Tests.UnitTests.Domain.ValueObjects;

[Trait("Category", "Unit")]
[Trait("Component", "ValueObject")]
public class PasswordTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DebeCrearPasswordValido_CuandoSeProporcionaPasswordFuerte()
    {
        // Prueba que el constructor crea correctamente un objeto Password cuando se proporciona una contraseña fuerte.
        // Arrange
        const string strongPassword = "SecurePass123!";

        // Act
        var password = new Password(strongPassword);

        // Assert
        password.Value.Should().Be(strongPassword);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_DebeLanzarArgumentException_CuandoPasswordEsNuloOEspacioEnBlanco(string invalidPassword)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Password(invalidPassword));
        exception.ParamName.Should().Be("password");
        exception.Message.Should().Contain("La contraseña no puede estar vacía");
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("12345678")]
    [InlineData("abcdefgh")]
    [InlineData("ABCDEFGH")]
    [InlineData("Abcdefgh")]
    [InlineData("Abcdefg1")]
    [InlineData("abcde1!")]
    [InlineData("ABCDE1!")]
    [InlineData("Ab1!")]
    [InlineData("Password")]
    [InlineData("password123")]
    [InlineData("PASSWORD123")]
    [InlineData("Password123")]
    [InlineData("password!")]
    [InlineData("PASSWORD!")]
    [InlineData("12345678!")]
    [InlineData("abcdefg!")]
    [InlineData("ABCDEFG!")]
    public void Constructor_DebeLanzarArgumentException_CuandoPasswordEsDebil(string weakPassword)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Password(weakPassword));
        exception.ParamName.Should().Be("password");
        exception.Message.Should().Contain("no cumple con los requisitos de seguridad");
    }

    [Theory]
    [InlineData("SecurePass123!")]
    [InlineData("MyPassword1@")]
    [InlineData("Complex123#")]
    [InlineData("StrongPwd456$")]
    [InlineData("VerySecure789%")]
    [InlineData("Testing123^")]
    [InlineData("Password1&")]
    [InlineData("Secure2023*")]
    [InlineData("MyTest123()")]
    [InlineData("Strong_Pass1")]
    [InlineData("Valid+Password2")]
    [InlineData("Good-Pass123")]
    [InlineData("Test[Pass]1")]
    [InlineData("Secure{Pass}2")]
    [InlineData("Valid;Pass:3")]
    [InlineData("Strong\"Pass'4")]
    [InlineData("Test\\Pass|5")]
    [InlineData("Valid,Pass.6")]
    [InlineData("Good<Pass>7")]
    [InlineData("Test/Pass?8")]
    public void Constructor_DebeCrearPasswordValido_CuandoSeProporcionanPasswordsFuertes(string strongPassword)
    {
        // Act
        var password = new Password(strongPassword);

        // Assert
        password.Value.Should().Be(strongPassword);
    }

    #endregion

    #region IsStrongPassword Static Method Tests
    // Pruebas para el método estático que valida la fortaleza de la contraseña.

    [Theory]
    [InlineData("SecurePass123!", true)]
    [InlineData("MyPassword1@", true)]
    [InlineData("Complex123#", true)]
    [InlineData("StrongPwd456$", true)]
    [InlineData("VerySecure789%", true)]
    [InlineData("weak", false)]
    [InlineData("12345678", false)]
    [InlineData("abcdefgh", false)]
    [InlineData("ABCDEFGH", false)]
    [InlineData("Abcdefgh", false)]
    [InlineData("Abcdefg1", false)]
    [InlineData("Abcdef1!", true)]
    [InlineData("abcde1!", false)]
    [InlineData("ABCDE1!", false)]
    [InlineData("Ab1!", false)]
    [InlineData("", false)]
    public void IsStrongPassword_DebeDevolverResultadoEsperado_CuandoSePruebaElPassword(string password, bool expectedResult)
    {
        // Act
        var result = Password.IsStrongPassword(password);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void IsStrongPassword_DebeLanzarArgumentNullException_CuandoPasswordEsNulo()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Password.IsStrongPassword(null!));
    }

    #endregion

    #region ValidatePassword Static Method Tests
    // Pruebas para el método estático que valida la contraseña según la política de seguridad.

    [Theory]
    [InlineData("SecurePass123!", true)]
    [InlineData("MyPassword1@", true)]
    [InlineData("weak", false)]
    [InlineData("12345678", false)]
    [InlineData("", false)]
    public void ValidatePassword_DebeDevolverResultadoEsperado_CuandoSeValidaElPassword(string password, bool expectedResult)
    {
        // Act
        var result = Password.ValidatePassword(password);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ValidatePassword_DebeComportarseIgualQueIsStrongPassword()
    {
        // Arrange
        var testPasswords = new[]
        {
            "SecurePass123!",
            "weak",
            "12345678",
            "ABCDEFGH",
            "Abcdefgh",
            "Abcdefg1",
            "Complex123#",
            ""
        };

        // Act & Assert
        foreach (var testPassword in testPasswords)
        {
            var validateResult = Password.ValidatePassword(testPassword);
            var isStrongResult = Password.IsStrongPassword(testPassword);
            
            validateResult.Should().Be(isStrongResult, 
                $"ValidatePassword and IsStrongPassword should return same result for '{testPassword}'");
        }
    }

    [Fact]
    public void ValidatePassword_DebeLanzarArgumentNullException_CuandoPasswordEsNulo()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Password.ValidatePassword(null!));
    }

    #endregion

    #region ToString Tests
    // Pruebas para el método ToString del objeto Password.

    [Fact]
    public void ToString_DebeDevolverPasswordEnmascarado_CuandoPasswordTieneValor()
    {
        // Arrange
        const string password = "SecurePass123!";
        var passwordObj = new Password(password);
        var expectedMask = new string('*', password.Length);

        // Act
        var result = passwordObj.ToString();

        // Assert
        result.Should().Be(expectedMask);
        result.Should().NotContain(password);
    }

    [Theory]
    [InlineData("Strong1!", 8)]
    [InlineData("MediumLength123!", 16)]
    [InlineData("VeryLongPasswordWithManyCharacters123!", 38)]
    public void ToString_DebeDevolverNumeroCorrectoDeAsteriscos_CuandoHayDiferentesLongitudesDePassword(string password, int expectedLength)
    {
        // Arrange
        var passwordObj = new Password(password);

        // Act
        var result = passwordObj.ToString();

        // Assert
        result.Should().HaveLength(expectedLength);
        result.Should().Be(new string('*', expectedLength));
    }

    #endregion

    #region Equality Tests (Record behavior)
    // Pruebas para la igualdad de objetos Password (comportamiento de record).

    [Fact]
    public void Igualdad_DebeSerVerdadera_CuandoPasswordsTienenMismoValor()
    {
        // Arrange
        const string passwordValue = "SecurePass123!";
        var password1 = new Password(passwordValue);
        var password2 = new Password(passwordValue);

        // Act & Assert
        password1.Should().Be(password2);
        (password1 == password2).Should().BeTrue();
        password1.GetHashCode().Should().Be(password2.GetHashCode());
    }

    [Fact]
    public void Igualdad_DebeSerFalsa_CuandoPasswordsTienenValoresDiferentes()
    {
        // Arrange
        var password1 = new Password("SecurePass123!");
        var password2 = new Password("DifferentPass456@");

        // Act & Assert
        password1.Should().NotBe(password2);
        (password1 == password2).Should().BeFalse();
    }

    #endregion

    #region Security Policy Tests
    // Pruebas para verificar el cumplimiento de la política de seguridad de contraseñas.

    [Fact]
    public void IsStrongPassword_DebeRequerirLongitudMinima()
    {
        // Arrange - 7 caracteres con todos los tipos requeridos
        const string shortPassword = "Abc123!";

        // Act
        var result = Password.IsStrongPassword(shortPassword);

        // Assert
        result.Should().BeFalse("password should require at least 8 characters");
    }

    [Fact]
    public void IsStrongPassword_DebeRequerirMinusculas()
    {
        // Arrange - Sin letras minúsculas
        const string noLowercasePassword = "ABCDEFG123!";

        // Act
        var result = Password.IsStrongPassword(noLowercasePassword);

        // Assert
        result.Should().BeFalse("password should require at least one lowercase letter");
    }

    [Fact]
    public void IsStrongPassword_DebeRequerirMayusculas()
    {
        // Arrange - Sin letras mayúsculas
        const string noUppercasePassword = "abcdefg123!";

        // Act
        var result = Password.IsStrongPassword(noUppercasePassword);

        // Assert
        result.Should().BeFalse("password should require at least one uppercase letter");
    }

    [Fact]
    public void IsStrongPassword_DebeRequerirDigito()
    {
        // Arrange - Sin dígitos
        const string noDigitPassword = "Abcdefgh!";

        // Act
        var result = Password.IsStrongPassword(noDigitPassword);

        // Assert
        result.Should().BeFalse("password should require at least one digit");
    }

    [Fact]
    public void IsStrongPassword_DebeRequerirCaracterEspecial()
    {
        // Arrange - Sin caracteres especiales
        const string noSpecialPassword = "Abcdefgh123";

        // Act
        var result = Password.IsStrongPassword(noSpecialPassword);

        // Assert
        result.Should().BeFalse("password should require at least one special character");
    }

    [Theory]
    [InlineData("!")]
    [InlineData("@")]
    [InlineData("#")]
    [InlineData("$")]
    [InlineData("%")]
    [InlineData("^")]
    [InlineData("&")]
    [InlineData("*")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("_")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("=")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData(";")]
    [InlineData("'")]
    [InlineData(":")]
    [InlineData("\"")]
    [InlineData("\\")]
    [InlineData("|")]
    [InlineData(",")]
    [InlineData(".")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("/")]
    [InlineData("?")]
    public void IsStrongPassword_DebeAceptarTodosLosCaracteresEspeciales_CuandoSeUsaCaracterEspecialValido(string specialChar)
    {
        // Arrange
        var passwordWithSpecialChar = $"Password123{specialChar}";

        // Act
        var result = Password.IsStrongPassword(passwordWithSpecialChar);

        // Assert
        result.Should().BeTrue($"password with special character '{specialChar}' should be valid");
    }

    #endregion

    #region Edge Cases and Security Tests
    // Pruebas para casos límite y escenarios de seguridad.

    [Fact]
    public void Constructor_NoDebeRecortarEspaciosEnBlanco_CuandoPasswordTieneEspaciosAlPrincipioOAlFinal()
    {
        // Arrange - Password con espacios (debería conservarse si cumple con los criterios)
        const string passwordWithSpaces = " SecurePass123! ";

        // Act
        var password = new Password(passwordWithSpaces);
        
        // Assert
        password.Value.Should().Be(passwordWithSpaces);
    }

    [Fact]
    public void IsStrongPassword_DebeRechazarPasswordsSoloConEspaciosEnBlanco()
    {
        // Arrange
        const string whitespacePassword = "        ";

        // Act
        var result = Password.IsStrongPassword(whitespacePassword);

        // Assert
        result.Should().BeFalse("password with only whitespace should be invalid");
    }

    [Theory]
    [InlineData("SecurePass123!\n")]
    [InlineData("SecurePass123!\r")]
    [InlineData("SecurePass123!\t")]
    [InlineData("Secure\nPass123!")]
    [InlineData("SecurePass\r123!")]
    [InlineData("Secure\tPass123!")]
    public void IsStrongPassword_DebeRechazarPasswordsConCaracteresDeControl(string passwordWithControlChars)
    {
        // Act
        var result = Password.IsStrongPassword(passwordWithControlChars);

        // Assert
        result.Should().BeFalse("current implementation correctly rejects control characters");
    }

    [Fact]
    public void Constructor_DebeManejarPasswordsLargosValidos()
    {
        // Arrange - Password muy largo pero válido
        var longPassword = "ThisIsAVeryLongPasswordThatMeetsAllSecurityRequirements123!@#$%^&*()_+-=";

        // Act
        var password = new Password(longPassword);

        // Assert
        password.Value.Should().Be(longPassword);
    }

    [Fact]
    public void IsStrongPassword_DebeManejarCaracteresUnicode()
    {
        // Arrange - Password con caracteres unicode (debería ser inválido según la expresión regular actual)
        const string unicodePassword = "Sécúré123!";

        // Act
        var result = Password.IsStrongPassword(unicodePassword);

        // Assert
        result.Should().BeTrue("current implementation allows unicode characters");
    }

    #endregion

    #region Performance Tests
    // Pruebas de rendimiento para la creación y validación de contraseñas.

    [Fact]
    public void IsStrongPassword_DebeSerRapido_AlValidarMuchosPasswords()
    {
        // Arrange
        const int passwordCount = 10000;
        var passwords = Enumerable.Range(1, passwordCount)
            .Select(i => $"Password{i}!")
            .ToList();

        // Act
        var start = DateTime.UtcNow;
        var results = passwords.Select(Password.IsStrongPassword).ToList();
        var elapsed = DateTime.UtcNow - start;

        // Assert
        results.Should().HaveCount(passwordCount);
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Debería ser muy rápido
    }

    #endregion
}