namespace Accesia.Tests.UnitTests.Domain.ValueObjects;

[Trait("Category", "Unit")]
[Trait("Component", "ValueObject")]
public class DeviceInfoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DeberiaCrearDeviceInfoValido_CuandoSeProporcionanParametrosValidos()
    {
        // Prueba que el constructor crea correctamente un objeto DeviceInfo con parámetros válidos.
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        const DeviceType deviceType = DeviceType.Desktop;
        const string browser = "Chrome";
        const string browserVersion = "96.0";
        const string operatingSystem = "Windows";
        const string deviceFingerprint = "abcd1234efgh5678";

        // Act
        var deviceInfo = new DeviceInfo(userAgent, deviceType, browser, browserVersion, operatingSystem, deviceFingerprint);

        // Assert
        deviceInfo.UserAgent.Should().Be(userAgent);
        deviceInfo.DeviceType.Should().Be(deviceType);
        deviceInfo.Browser.Should().Be(browser);
        deviceInfo.BrowserVersion.Should().Be(browserVersion);
        deviceInfo.OperatingSystem.Should().Be(operatingSystem);
        deviceInfo.DeviceFingerprint.Should().Be(deviceFingerprint);
    }

    [Fact]
    public void Constructor_DeberiaLanzarArgumentNullException_CuandoUserAgentEsNulo()
    {
        // Prueba que el constructor lanza ArgumentNullException cuando el userAgent es nulo.
        // Arrange
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DeviceInfo(null!, DeviceType.Desktop, "Chrome", "96.0", "Windows", "fingerprint"));
        
        exception.ParamName.Should().Be("userAgent");
    }

    [Fact]
    public void Constructor_DeberiaLanzarArgumentNullException_CuandoDeviceFingerprintEsNulo()
    {
        // Prueba que el constructor lanza ArgumentNullException cuando el deviceFingerprint es nulo.
        // Arrange
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DeviceInfo("userAgent", DeviceType.Desktop, "Chrome", "96.0", "Windows", null!));
        
        exception.ParamName.Should().Be("deviceFingerprint");
    }

    [Fact]
    public void Constructor_DeberiaEstablecerValoresPorDefecto_CuandoParametrosOpcionalesSonNulos()
    {
        // Prueba que el constructor establece valores predeterminados cuando los parámetros opcionales son nulos.
        // Act
        var deviceInfo = new DeviceInfo("userAgent", DeviceType.Desktop, null, null, null, "fingerprint");

        // Assert
        deviceInfo.Browser.Should().Be("Unknown");
        deviceInfo.BrowserVersion.Should().Be("Unknown");
        deviceInfo.OperatingSystem.Should().Be("Unknown");
    }

    [Fact]
    public void ConstructorSinParametros_DeberiaCrearDeviceInfoConValoresVacios()
    {
        // Prueba que el constructor sin parámetros crea un objeto DeviceInfo con valores vacíos.
        // Act
        var deviceInfo = new DeviceInfo();

        // Assert
        deviceInfo.UserAgent.Should().Be(string.Empty);
        deviceInfo.DeviceType.Should().Be(default(DeviceType));
        deviceInfo.Browser.Should().Be(string.Empty);
        deviceInfo.BrowserVersion.Should().Be(string.Empty);
        deviceInfo.OperatingSystem.Should().Be(string.Empty);
        deviceInfo.DeviceFingerprint.Should().Be(string.Empty);
    }

    #endregion

    #region CreateFromUserAgent Tests
    // Pruebas para el método de creación a partir del user agent.

    [Fact]
    public void CreateFromUserAgent_DeberiaCrearDeviceInfoValido_CuandoSeProporcionaUserAgentValido()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36";

        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.Should().NotBeNull();
        deviceInfo.UserAgent.Should().Be(userAgent);
        deviceInfo.DeviceFingerprint.Should().NotBeNullOrEmpty();
        deviceInfo.DeviceFingerprint.Should().HaveLength(16);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateFromUserAgent_DeberiaLanzarArgumentException_CuandoUserAgentEsNuloOEspacioEnBlanco(string invalidUserAgent)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DeviceInfo.CreateFromUserAgent(invalidUserAgent));
        exception.ParamName.Should().Be("userAgent");
        exception.Message.Should().Contain("User agent no puede estar vacío");
    }

    #endregion

    #region Device Type Detection Tests
    // Pruebas para la detección del tipo de dispositivo.

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)", DeviceType.Mobile)]
    [InlineData("Mozilla/5.0 (Linux; Android 10; SM-G973F)", DeviceType.Mobile)]
    [InlineData("Mozilla/5.0 (Mobile; rv:40.0) Gecko/40.0 Firefox/40.0", DeviceType.Mobile)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X)", DeviceType.Tablet)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", DeviceType.Desktop)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", DeviceType.Desktop)]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", DeviceType.Desktop)]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)", DeviceType.Bot)]
    [InlineData("Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)", DeviceType.Bot)]
    [InlineData("Mozilla/5.0 (compatible; Yahoo! Slurp; http://help.yahoo.com/help/us/ysearch/slurp)", DeviceType.Desktop)]
    public void CreateFromUserAgent_DeberiaDetectarTipoDispositivoCorrecto_CuandoSeProporcionanDiferentesUserAgents(string userAgent, DeviceType expectedDeviceType)
    {
        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.DeviceType.Should().Be(expectedDeviceType);
    }

    #endregion

    #region Browser Detection Tests
    // Pruebas para la detección del navegador.

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36", "Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.1 Safari/605.1.15", "Safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36 Edg/96.0.1054.62", "Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36 OPR/82.0.4227.43", "Chrome")]
    [InlineData("SomeUnknownBrowser/1.0", "Unknown")]
    public void CreateFromUserAgent_DeberiaDetectarNavegadorCorrecto_CuandoSeProporcionanDiferentesUserAgents(string userAgent, string expectedBrowser)
    {
        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.Browser.Should().Be(expectedBrowser);
    }

    #endregion

    #region Operating System Detection Tests
    // Pruebas para la detección del sistema operativo.

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "macOS")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", "Linux")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; SM-G973F)", "Linux")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)", "macOS")]
    [InlineData("SomeUnknownOS/1.0", "Unknown")]
    public void CreateFromUserAgent_DeberiaDetectarSOCorrecto_CuandoSeProporcionanDiferentesUserAgents(string userAgent, string expectedOS)
    {
        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.OperatingSystem.Should().Be(expectedOS);
    }

    #endregion

    #region Fingerprint Generation Tests
    // Pruebas para la generación del fingerprint del dispositivo.

    [Fact]
    public void CreateFromUserAgent_DeberiaGenerarFingerprintConsistente_CuandoSeUsaMismoUserAgent()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        // Act
        var deviceInfo1 = DeviceInfo.CreateFromUserAgent(userAgent);
        var deviceInfo2 = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo1.DeviceFingerprint.Should().Be(deviceInfo2.DeviceFingerprint);
    }

    [Fact]
    public void CreateFromUserAgent_DeberiaGenerarFingerprintsDiferentes_CuandoSeUsanDiferentesUserAgents()
    {
        // Arrange
        const string userAgent1 = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/96.0";
        const string userAgent2 = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Safari/605.1";

        // Act
        var deviceInfo1 = DeviceInfo.CreateFromUserAgent(userAgent1);
        var deviceInfo2 = DeviceInfo.CreateFromUserAgent(userAgent2);

        // Assert
        deviceInfo1.DeviceFingerprint.Should().NotBe(deviceInfo2.DeviceFingerprint);
    }

    [Fact]
    public void CreateFromUserAgent_DeberiaGenerarFingerprintHexadecimal()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.DeviceFingerprint.Should().MatchRegex("^[A-F0-9]{16}$");
    }

    #endregion

    #region Edge Cases Tests
    // Pruebas para casos límite de DeviceInfo.

    [Fact]
    public void CreateFromUserAgent_DeberiaManejarUserAgentVacioConGracia_DespuesDeValidacion()
    {
        // This test ensures that if somehow an empty user agent passes validation,
        // the system handles it gracefully
        // The validation should catch this, so we expect an exception
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeviceInfo.CreateFromUserAgent(""));
    }

    [Fact]
    public void CreateFromUserAgent_DeberiaManejarUserAgentMuyLargo()
    {
        // Arrange
        var longUserAgent = new string('a', 1000) + " Chrome/96.0 Windows";

        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(longUserAgent);

        // Assert
        deviceInfo.UserAgent.Should().Be(longUserAgent);
        deviceInfo.Browser.Should().Be("Chrome");
        deviceInfo.OperatingSystem.Should().Be("Windows");
        deviceInfo.DeviceFingerprint.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateFromUserAgent_DeberiaManejarUserAgentConCaracteresEspeciales()
    {
        // Arrange
        const string userAgentWithSpecialChars = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.3) Gecko/20100401 Firefox/3.6.3 (.NET CLR 3.5.30729)";

        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgentWithSpecialChars);

        // Assert
        deviceInfo.UserAgent.Should().Be(userAgentWithSpecialChars);
        deviceInfo.Browser.Should().Be("Firefox");
        deviceInfo.OperatingSystem.Should().Be("Windows");
    }

    [Theory]
    [InlineData("mozilla/5.0 (windows nt 10.0; win64; x64) chrome/96.0", "Chrome", "Windows")]
    [InlineData("MOZILLA/5.0 (WINDOWS NT 10.0; WIN64; X64) CHROME/96.0", "Chrome", "Windows")]
    [InlineData("Mozilla/5.0 (ANDROID 10; MOBILE) FIREFOX/94.0", "Firefox", "Android")]
    public void CreateFromUserAgent_DeberiaSerInsensibleAMayusculas_AlDetectarNavegadorYSO(string userAgent, string expectedBrowser, string expectedOS)
    {
        // Act
        var deviceInfo = DeviceInfo.CreateFromUserAgent(userAgent);

        // Assert
        deviceInfo.Browser.Should().Be(expectedBrowser);
        deviceInfo.OperatingSystem.Should().Be(expectedOS);
    }

    #endregion

    #region Performance Tests
    // Pruebas de rendimiento para DeviceInfo.

    [Fact]
    public void CreateFromUserAgent_DeberiaSerRapido_AlProcesarMuchosUserAgents()
    {
        // Arrange
        var userAgents = Enumerable.Range(1, 100)
            .Select(i => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/96.{i}")
            .ToList();

        // Act
        var start = DateTime.UtcNow;
        var deviceInfos = userAgents.Select(DeviceInfo.CreateFromUserAgent).ToList();
        var elapsed = DateTime.UtcNow - start;

        // Assert
        deviceInfos.Should().HaveCount(100);
        deviceInfos.Should().AllSatisfy(di => di.DeviceFingerprint.Should().NotBeNullOrEmpty());
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    #endregion
}