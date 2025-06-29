namespace Accesia.Tests.UnitTests.Domain.ValueObjects;

[Trait("Category", "Unit")]
[Trait("Component", "ValueObject")]
public class EmailTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DebeCrearEmailValido_CuandoSeProporcionaEmailValido()
    {
        // Prueba que el constructor crea correctamente un objeto Email cuando se proporciona un email válido.
        // Arrange
        const string validEmail = "test@example.com";

        // Act
        var email = new Email(validEmail);

        // Assert
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Fact]
    public void Constructor_DebeNormalizarEmail_CuandoEmailTieneMayusculas()
    {
        // Prueba que el constructor normaliza el email cuando contiene mayúsculas.
        // Arrange
        const string mixedCaseEmail = "Test.User@EXAMPLE.COM";
        const string expectedNormalized = "test.user@example.com";

        // Act
        var email = new Email(mixedCaseEmail);

        // Assert
        email.Value.Should().Be(expectedNormalized);
    }

    [Fact]
    public void Constructor_DebeRecortarEspaciosEnBlanco_CuandoEmailTieneEspacios()
    {
        // Prueba que el constructor elimina los espacios en blanco alrededor del email.
        // Arrange
        const string emailWithSpaces = "  test@example.com  ";
        const string expectedTrimmed = "test@example.com";

        // Act
        var email = new Email(emailWithSpaces);

        // Assert
        email.Value.Should().Be(expectedTrimmed);
    }

    #endregion

    #region Validation Tests
    // Pruebas para la validación de emails.

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_DebeLanzarArgumentException_CuandoEmailEsNuloOEspacioEnBlanco(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
        exception.ParamName.Should().Be("email");
        exception.Message.Should().Contain("El email no puede estar vacío");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    [InlineData("test@example")]
    [InlineData("test@.com")]
    [InlineData("test@example.")]

    [InlineData("test@exam ple.com")]
    [InlineData("test @example.com")]
    [InlineData("test@example.c")]
    [InlineData("test@@example.com")]
    [InlineData("test@example@com")]
    public void Constructor_DebeLanzarArgumentException_CuandoFormatoDeEmailEsInvalido(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
        exception.ParamName.Should().Be("email");
        exception.Message.Should().Contain("El formato del email no es válido");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("test123@example123.com")]
    [InlineData("user_name@example-site.org")]
    [InlineData("test.email+filter@subdomain.example.com")]
    [InlineData("a@b.co")]
    [InlineData("very.long.email.address@very.long.domain.example.com")]
    [InlineData("user%name@example.com")]
    [InlineData("user-name@example.com")]
    [InlineData("123@example.com")]
    [InlineData("user@123.com")]
    [InlineData("test..test@example.com")]
    [InlineData("test@example..com")]
    public void Constructor_DebeCrearEmailValido_CuandoSeProporcionanFormatosDeEmailValidos(string validEmail)
    {
        // Act
        var email = new Email(validEmail);

        // Assert
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    #endregion

    #region Static Factory Method Tests
    // Pruebas para el método de fábrica estático de Email.

    [Fact]
    public void Create_DeberiaRetornarEmailValido_CuandoSeProporcionaEmailValido()
    {
        // Arrange
        const string validEmail = "factory@example.com";

        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail);
    }

    [Fact]
    public void Create_DeberiaLanzarExcepcion_CuandoSeProporcionaEmailInvalido()
    {
        // Arrange
        const string invalidEmail = "invalid-email";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
    }

    #endregion

    #region ToString Tests
    // Pruebas para el método ToString del objeto Email.

    [Fact]
    public void ToString_DeberiaRetornarValorDeEmail()
    {
        // Arrange
        const string emailValue = "test@example.com";
        var email = new Email(emailValue);

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be(emailValue);
    }

    #endregion

    #region Equality Tests (Record behavior)
    // Pruebas para la igualdad de objetos Email (comportamiento de record).

    [Fact]
    public void Equality_DeberiaSerTrue_CuandoEmailsTienenElMismoValor()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("TEST@EXAMPLE.COM"); // Should be normalized

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void Equality_DeberiaSerFalse_CuandoEmailsTienenValoresDiferentes()
    {
        // Arrange
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
        (email1 == email2).Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Security Tests
    // Pruebas para casos límite y escenarios de seguridad relacionados con emails.

    [Theory]
    [InlineData("test@example.com", "test@example.com")]
    [InlineData("Test@Example.Com", "test@example.com")]
    [InlineData("  TEST@EXAMPLE.COM  ", "test@example.com")]
    [InlineData("Test.User+Tag@Sub.Example.Com", "test.user+tag@sub.example.com")]
    public void Constructor_DeberiaProducirNormalizacionConsistente(string input, string expectedOutput)
    {
        // Act
        var email = new Email(input);

        // Assert
        email.Value.Should().Be(expectedOutput);
    }

    [Fact]
    public void Constructor_DeberiaManejarEmailsValidosLargos()
    {
        // Arrange - Longitud máxima práctica de un email
        var longLocalPart = new string('a', 64); // La parte local máxima es de 64 caracteres
        var longDomainPart = "very.long.subdomain.example.com";
        var longEmail = $"{longLocalPart}@{longDomainPart}";

        // Act
        var email = new Email(longEmail);

        // Assert
        email.Value.Should().Be(longEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("test@example.com\n")]
    [InlineData("test@example.com\r")]
    [InlineData("test@example.com\t")]
    [InlineData("test\n@example.com")]
    [InlineData("test@exam\nple.com")]
    public void Constructor_DeberiaRechazarEmailsConCaracteresDeControl(string emailWithControlChars)
    {
        // La implementación actual rechaza correctamente los caracteres de control
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(emailWithControlChars));
    }

    #endregion

    #region Performance Tests
    // Pruebas de rendimiento para la creación y validación de emails.

    [Fact]
    public void Constructor_DeberiaSerRapido_CuandoSeProcesanMuchosEmails()
    {
        // Arrange
        var emails = Enumerable.Range(1, 1000)
            .Select(i => $"user{i}@example{i % 10}.com")
            .ToList();

        // Act
        var start = DateTime.UtcNow;
        var emailObjects = emails.Select(e => new Email(e)).ToList();
        var elapsed = DateTime.UtcNow - start;

        // Assert
        emailObjects.Should().HaveCount(1000);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Deberia ser muy rápido
    }

    #endregion
}