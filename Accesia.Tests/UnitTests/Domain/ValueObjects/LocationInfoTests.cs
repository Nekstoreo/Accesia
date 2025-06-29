namespace Accesia.Tests.UnitTests.Domain.ValueObjects;

[Trait("Category", "Unit")]
[Trait("Component", "ValueObject")]
public class LocationInfoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DebeCrearLocationInfoValido_CuandoSeProporcionanParametrosValidos()
    {
        // Prueba que el constructor crea correctamente un objeto LocationInfo con parámetros válidos.
        // Arrange
        const string ipAddress = "192.168.1.1";
        const string country = "Colombia";
        const string city = "Bogotá";
        const string region = "Cundinamarca";
        const string isp = "Claro";
        const bool isVPN = false;

        // Act
        var locationInfo = new LocationInfo(ipAddress, country, city, region, isp, isVPN);

        // Assert
        locationInfo.IpAddress.Should().Be(ipAddress);
        locationInfo.Country.Should().Be(country);
        locationInfo.City.Should().Be(city);
        locationInfo.Region.Should().Be(region);
        locationInfo.ISP.Should().Be(isp);
        locationInfo.IsVPN.Should().Be(isVPN);
    }

    [Fact]
    public void Constructor_DebeCrearLocationInfoValido_CuandoSoloSeProporcionaDireccionIp()
    {
        // Prueba que el constructor crea correctamente un objeto LocationInfo cuando solo se proporciona la dirección IP.
        // Arrange
        const string ipAddress = "8.8.8.8";

        // Act
        var locationInfo = new LocationInfo(ipAddress);

        // Assert
        locationInfo.IpAddress.Should().Be(ipAddress);
        locationInfo.Country.Should().BeNull();
        locationInfo.City.Should().BeNull();
        locationInfo.Region.Should().BeNull();
        locationInfo.ISP.Should().BeNull();
        locationInfo.IsVPN.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_DebeLanzarArgumentException_CuandoDireccionIpEsNulaOEspacioEnBlanco(string invalidIpAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocationInfo(invalidIpAddress));
        exception.ParamName.Should().Be("ipAddress");
        exception.Message.Should().Contain("IP address no puede estar vacía");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("256.256.256.256")]

    [InlineData("192.168.1.1.1")]
    [InlineData("192.168.1.256")]
    [InlineData("192.168.-1.1")]
    [InlineData("abc.def.ghi.jkl")]
    [InlineData("192.168.1.1:8080")]
    [InlineData("http://192.168.1.1")]
    public void Constructor_DebeLanzarArgumentException_CuandoFormatoDeDireccionIpEsInvalido(string invalidIpAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocationInfo(invalidIpAddress));
        exception.ParamName.Should().Be("ipAddress");
        exception.Message.Should().Contain("IP address no tiene un formato válido");
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("8.8.8.8")]
    [InlineData("208.67.222.222")]
    [InlineData("1.1.1.1")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("fe80::1")]
    [InlineData("192.168.1")]
    public void Constructor_DebeCrearLocationInfoValido_CuandoSeProporcionanFormatosDeDireccionIpValidos(string validIpAddress)
    {
        // Act
        var locationInfo = new LocationInfo(validIpAddress);

        // Assert
        locationInfo.IpAddress.Should().Be(validIpAddress);
    }

    [Fact]
    public void ConstructorSinParametros_DebeCrearLocationInfoConValoresVacios()
    {
        // Act
        var locationInfo = new LocationInfo();

        // Assert
        locationInfo.IpAddress.Should().Be(string.Empty);
        locationInfo.Country.Should().BeNull();
        locationInfo.City.Should().BeNull();
        locationInfo.Region.Should().BeNull();
        locationInfo.ISP.Should().BeNull();
        locationInfo.IsVPN.Should().BeFalse();
    }

    #endregion

    #region CreateFromIpAddress Tests
    // Pruebas para el método de creación a partir de dirección IP.

    [Fact]
    public void CreateFromIpAddress_DebeCrearLocationInfoValido_CuandoSeProporcionaDireccionIpValida()
    {
        // Arrange
        const string ipAddress = "8.8.8.8";

        // Act
        var locationInfo = LocationInfo.CreateFromIpAddress(ipAddress);

        // Assert
        locationInfo.Should().NotBeNull();
        locationInfo.IpAddress.Should().Be(ipAddress);
        locationInfo.Country.Should().Be("Unknown");
        locationInfo.City.Should().Be("Unknown");
        locationInfo.Region.Should().Be("Unknown");
        locationInfo.ISP.Should().Be("Unknown");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateFromIpAddress_DebeLanzarArgumentException_CuandoDireccionIpEsNulaOEspacioEnBlanco(string invalidIpAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => LocationInfo.CreateFromIpAddress(invalidIpAddress));
        exception.ParamName.Should().Be("ipAddress");
        exception.Message.Should().Contain("IP address no puede estar vacía");
    }

    #endregion

    #region CreateLocalhost Tests
    // Pruebas para el método que crea una instancia para localhost.

    [Fact]
    public void CreateLocalhost_DebeCrearLocationInfoDeLocalhost()
    {
        // Act
        var locationInfo = LocationInfo.CreateLocalhost();

        // Assert
        locationInfo.IpAddress.Should().Be("127.0.0.1");
        locationInfo.Country.Should().Be("Local");
        locationInfo.City.Should().Be("Local");
        locationInfo.Region.Should().Be("Local");
        locationInfo.ISP.Should().Be("Local");
        locationInfo.IsVPN.Should().BeFalse();
    }

    #endregion

    #region IsLocalAddress Tests
    // Pruebas para el método que determina si la dirección es local.

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("208.67.222.222", false)]
    [InlineData("172.15.0.1", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("193.168.1.1", false)]
    [InlineData("11.0.0.1", false)]
    public void IsLocalAddress_DebeDevolverResultadoEsperado_CuandoSeProporcionanDiferentesDireccionesIp(string ipAddress, bool expectedResult)
    {
        // Arrange
        var locationInfo = new LocationInfo(ipAddress);

        // Act
        var result = locationInfo.IsLocalAddress();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("172.16.0.1", true)]  // First IP in range
    [InlineData("172.31.255.255", true)]  // Last IP in range
    [InlineData("172.20.100.50", true)]  // Middle of range
    [InlineData("172.15.255.255", false)]  // Just before range
    [InlineData("172.32.0.1", false)]  // Just after range
    public void IsLocalAddress_DebeIdentificarCorrectamenteClaseBPrivada_CuandoSeProporcionanDireccionesIpDeClaseB(string ipAddress, bool expectedResult)
    {
        // Arrange
        var locationInfo = new LocationInfo(ipAddress);

        // Act
        var result = locationInfo.IsLocalAddress();

        // Assert
        result.Should().Be(expectedResult, $"IP {ipAddress} should {(expectedResult ? "" : "not ")}be identified as private Class B");
    }

    #endregion

    #region GetDisplayLocation Tests
    // Pruebas para el método que obtiene la ubicación en formato de visualización.

    [Fact]
    public void GetDisplayLocation_DebeDevolverLocal_CuandoLaDireccionEsLocal()
    {
        // Arrange
        var locationInfo = new LocationInfo("127.0.0.1", "Colombia", "Bogotá", "Cundinamarca");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Local");
    }

    [Fact]
    public void GetDisplayLocation_DebeDevolverUbicacionFormateada_CuandoSeProporcionanTodosLosDatosDeUbicacion()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8", "Colombia", "Bogotá", "Cundinamarca");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Bogotá, Cundinamarca, Colombia");
    }

    [Fact]
    public void GetDisplayLocation_DebeDevolverUbicacionParcial_CuandoSeProporcionanAlgunosDatosDeUbicacion()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8", "Colombia", null, "Cundinamarca");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Cundinamarca, Colombia");
    }

    [Fact]
    public void GetDisplayLocation_DebeDevolverSoloPais_CuandoSoloSeProporcionaElPais()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8", "Colombia");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Colombia");
    }

    [Fact]
    public void GetDisplayLocation_DebeDevolverUbicacionDesconocida_CuandoNoSeProporcionanDatosDeUbicacion()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Unknown Location");
    }

    [Fact]
    public void GetDisplayLocation_DebeIgnorarCadenasVacias_CuandoLosDatosDeUbicacionEstanVacios()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8", "Colombia", "", "Cundinamarca");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Cundinamarca, Colombia");
    }

    [Fact]
    public void GetDisplayLocation_DebeIgnorarCadenasDeEspaciosEnBlanco_CuandoLosDatosDeUbicacionSonEspaciosEnBlanco()
    {
        // Arrange
        var locationInfo = new LocationInfo("8.8.8.8", "Colombia", "   ", "Cundinamarca");

        // Act
        var result = locationInfo.GetDisplayLocation();

        // Assert
        result.Should().Be("Cundinamarca, Colombia");
    }

    #endregion

    #region Edge Cases Tests
    // Pruebas para casos límite de LocationInfo.

    [Theory]
    [InlineData("2001:4860:4860::8888")]  // Google DNS IPv6
    [InlineData("2001:4860:4860::8844")]  // Google DNS IPv6
    [InlineData("2606:4700:4700::1111")]  // Cloudflare DNS IPv6
    [InlineData("fe80::1")]  // Link-local IPv6
    public void Constructor_DebeManejarDireccionesIPv6(string ipv6Address)
    {
        // Act
        var locationInfo = new LocationInfo(ipv6Address);

        // Assert
        locationInfo.IpAddress.Should().Be(ipv6Address);
    }

    [Fact]
    public void Constructor_DebeManejarNombresDeUbicacionLargos()
    {
        // Arrange
        const string ipAddress = "8.8.8.8";
        var longCountry = new string('A', 100);
        var longCity = new string('B', 100);
        var longRegion = new string('C', 100);
        var longISP = new string('D', 100);

        // Act
        var locationInfo = new LocationInfo(ipAddress, longCountry, longCity, longRegion, longISP, true);

        // Assert
        locationInfo.Country.Should().Be(longCountry);
        locationInfo.City.Should().Be(longCity);
        locationInfo.Region.Should().Be(longRegion);
        locationInfo.ISP.Should().Be(longISP);
        locationInfo.IsVPN.Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8", "País con acentos", "Ciudad con ñ", "Región con ü")]
    [InlineData("1.1.1.1", "国家", "城市", "地区")]
    [InlineData("208.67.222.222", "Страна", "Город", "Регион")]
    public void Constructor_DebeManejarCaracteresUnicode_CuandoNombresDeUbicacionContienenCaracteresEspeciales(
        string ipAddress, string country, string city, string region)
    {
        // Act
        var locationInfo = new LocationInfo(ipAddress, country, city, region);

        // Assert
        locationInfo.Country.Should().Be(country);
        locationInfo.City.Should().Be(city);
        locationInfo.Region.Should().Be(region);
    }

    [Fact]
    public void IsLocalAddress_DebeManejarLocalhostIPv6()
    {
        // Arrange
        var locationInfo = new LocationInfo("::1");

        // Act
        var result = locationInfo.IsLocalAddress();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Performance Tests
    // Pruebas de rendimiento para LocationInfo.

    [Fact]
    public void Constructor_DebeSerRapido_AlCrearMuchosLocationInfos()
    {
        // Arrange
        var ipAddresses = Enumerable.Range(1, 100)
            .Select(i => $"192.168.1.{i}")
            .ToList();

        // Act
        var start = DateTime.UtcNow;
        var locationInfos = ipAddresses.Select(ip => new LocationInfo(ip, "Country", "City", "Region")).ToList();
        var elapsed = DateTime.UtcNow - start;

        // Assert
        locationInfos.Should().HaveCount(100);
        locationInfos.Should().AllSatisfy(li => li.IpAddress.Should().StartWith("192.168.1."));
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsLocalAddress_DebeSerRapido_AlVerificarMuchasDirecciones()
    {
        // Arrange
        var locationInfos = Enumerable.Range(1, 100)
            .Select(i => new LocationInfo($"192.168.1.{i}"))
            .ToList();

        // Act
        var start = DateTime.UtcNow;
        var results = locationInfos.Select(li => li.IsLocalAddress()).ToList();
        var elapsed = DateTime.UtcNow - start;

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    #endregion
}