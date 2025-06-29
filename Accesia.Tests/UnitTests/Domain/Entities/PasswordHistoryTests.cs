namespace Accesia.Tests.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
public class PasswordHistoryTests
{
    #region Helpers para pruebas
    private static User CreateValidUser()
    {
        return new User(
            new Email("test@example.com"),
            "hashedpassword",
            "Test",
            "User")
        {
            Id = Guid.NewGuid(),
            Status = UserStatus.Active
        };
    }

    private static string GetValidPasswordHash()
    {
        return "$2a$10$dGVzdCBoYXNoIGZvciB0ZXN0aW5nIHB1cnBvc2VzIG9ubHkKUERCMTIz";
    }

    #endregion

    #region Pruebas de constructor
    [Fact]
    public void PasswordHistory_ConParametrosValidos_DeberiaCrearCorrectamente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();
        var beforeCreation = DateTime.UtcNow;

        // Act
        var passwordHistory = new PasswordHistory(userId, passwordHash);
        var afterCreation = DateTime.UtcNow;

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir con el proporcionado.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir con el proporcionado.");
        passwordHistory.PasswordChangedAt.Should().BeAfter(beforeCreation, "La fecha de cambio de contraseña debe ser posterior a la creación.");
        passwordHistory.PasswordChangedAt.Should().BeOnOrBefore(afterCreation, "La fecha de cambio de contraseña debe ser igual o anterior a la finalización de la creación.");
    }

    [Fact]
    public void PasswordHistory_ConUserIdVacio_DeberiaCrearIgualmente()
    {
        // Arrange
        var userId = Guid.Empty;
        var passwordHash = GetValidPasswordHash();

        // Act
        var passwordHistory = new PasswordHistory(userId, passwordHash);

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir con el proporcionado.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir con el proporcionado.");
    }

    [Fact]
    public void PasswordHistory_ConPasswordHashVacio_DeberiaCrearIgualmente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = string.Empty;

        // Act
        var passwordHistory = new PasswordHistory(userId, passwordHash);

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir con el proporcionado.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir con el proporcionado.");
    }

    [Fact]
    public void PasswordHistory_DeberiaEstablecerPasswordChangedAtAUUtcNow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        // Act
        var passwordHistory = new PasswordHistory(userId, passwordHash);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        passwordHistory.PasswordChangedAt.Should().BeAfter(beforeCreation, "La fecha de cambio de contraseña debe ser posterior a la hora de pre-creación.");
        passwordHistory.PasswordChangedAt.Should().BeBefore(afterCreation, "La fecha de cambio de contraseña debe ser anterior a la hora de post-creación.");
        passwordHistory.PasswordChangedAt.Kind.Should().Be(DateTimeKind.Utc, "La Kind de la fecha de cambio de contraseña debe ser Utc.");
    }

    #endregion

    #region Pruebas de métodos de fábrica
    [Fact]
    public void Create_ConParametrosValidos_DeberiaCrearPasswordHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();

        // Act
        var passwordHistory = PasswordHistory.Create(userId, passwordHash);

        // Assert
        passwordHistory.Should().NotBeNull("El PasswordHistory creado no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir.");
        passwordHistory.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "La fecha de cambio de contraseña debe ser cercana a la hora actual.");
    }

    [Fact]
    public void Create_DeberiaComportarseIdenticoAlConstructor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();

        // Act
        var historyFromConstructor = new PasswordHistory(userId, passwordHash);
        var historyFromFactory = PasswordHistory.Create(userId, passwordHash);

        // Assert
        historyFromConstructor.UserId.Should().Be(historyFromFactory.UserId, "El UserId de la construcción debe ser el mismo que el de la fábrica.");
        historyFromConstructor.PasswordHash.Should().Be(historyFromFactory.PasswordHash, "El PasswordHash de la construcción debe ser el mismo que el de la fábrica.");
        historyFromConstructor.PasswordChangedAt.Should().BeCloseTo(historyFromFactory.PasswordChangedAt, TimeSpan.FromSeconds(1), "Las marcas de tiempo de la construcción y la fábrica deben ser similares.");
    }

    [Theory]
    [InlineData("$2a$10$simple.hash")]
    [InlineData("$2b$12$very.long.complex.password.hash.with.many.characters.for.testing")]
    [InlineData("bcrypt$hash$format")]
    [InlineData("plain_text_hash_for_testing")]
    public void Create_ConDiferentesHashesDeContraseña_DeberiaCrearCorrectamente(string passwordHash)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var passwordHistory = PasswordHistory.Create(userId, passwordHash);

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir.");
    }

    #endregion

    #region Pruebas de propiedades y asignación
    [Fact]
    public void UserId_DeberiaSerAsignable()
    {
        // Arrange
        var originalUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var passwordHistory = new PasswordHistory(originalUserId, GetValidPasswordHash());

        // Act
        passwordHistory.UserId = newUserId;

        // Assert
        passwordHistory.UserId.Should().Be(newUserId, "El UserId debería haberse actualizado al nuevo valor.");
        passwordHistory.UserId.Should().NotBe(originalUserId, "El UserId no debería ser el valor original.");
    }

    [Fact]
    public void PasswordHash_DeberiaSerAsignable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var originalHash = GetValidPasswordHash();
        var newHash = "$2a$10$new.hash.for.testing.purposes";
        var passwordHistory = new PasswordHistory(userId, originalHash);

        // Act
        passwordHistory.PasswordHash = newHash;

        // Assert
        passwordHistory.PasswordHash.Should().Be(newHash, "El PasswordHash debería haberse actualizado al nuevo valor.");
        passwordHistory.PasswordHash.Should().NotBe(originalHash, "El PasswordHash no debería ser el valor original.");
    }

    [Fact]
    public void PasswordChangedAt_DeberiaSerAsignable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHistory = new PasswordHistory(userId, GetValidPasswordHash());
        var newChangeTime = DateTime.UtcNow.AddDays(-5);

        // Act
        passwordHistory.PasswordChangedAt = newChangeTime;

        // Assert
        passwordHistory.PasswordChangedAt.Should().Be(newChangeTime, "La fecha de cambio de contraseña debería haberse actualizado.");
    }

    [Fact]
    public void PasswordHistory_DeberiaInicializarPropiedadDeNavegacionUser()
    {
        // Arrange & Act
        var passwordHistory = new PasswordHistory(Guid.NewGuid(), GetValidPasswordHash());

        // Assert
        // La propiedad User debería ser inicializada a null! (tipo de referencia no nulable)
        // No podemos probar la asignación nula directamente debido a los tipos de referencia nulables
        // Pero podemos verificar que la propiedad existe y puede ser establecida
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        
        var user = CreateValidUser();
        passwordHistory.User = user;
        passwordHistory.User.Should().Be(user, "La propiedad de navegación User debe ser la establecida.");
    }

    #endregion

    #region Pruebas de entidad auditable
    [Fact]
    public void PasswordHistory_DeberiaHeredarDeAuditableEntity()
    {
        // Arrange & Act
        var passwordHistory = new PasswordHistory(Guid.NewGuid(), GetValidPasswordHash());

        // Assert
        passwordHistory.Should().BeAssignableTo<AuditableEntity>("PasswordHistory debe heredar de AuditableEntity.");
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");

        // Propiedades de AuditableEntity disponibles (Id comienza como Empty en pruebas unitarias)
        passwordHistory.Id.Should().Be(Guid.Empty, "El Id debe ser Guid.Empty al inicio en pruebas unitarias.");
        
        // Pero podemos establecer el Id manualmente
        var newId = Guid.NewGuid();
        passwordHistory.Id = newId;
        passwordHistory.Id.Should().Be(newId, "El Id debe poder ser asignado manualmente.");
    }

    [Fact]
    public void PasswordHistory_DeberiaTenerPropiedadesAuditables()
    {
        // Arrange & Act
        var passwordHistory = new PasswordHistory(Guid.NewGuid(), GetValidPasswordHash());

        // Assert
        var now = DateTime.UtcNow;
        var createdBy = "test-user-id";
        
        passwordHistory.CreatedAt = now;
        passwordHistory.CreatedBy = createdBy;
        
        passwordHistory.CreatedAt.Should().Be(now, "CreatedAt debería ser la fecha actual.");
        passwordHistory.CreatedBy.Should().Be(createdBy, "CreatedBy debería ser el usuario de prueba.");
    }

    #endregion

    #region Pruebas de atributos de validación
    [Fact]
    public void PasswordHistory_AtributosRequeridos_DeberianEstarPresentes()
    {
        // Arrange
        var type = typeof(PasswordHistory);

        // Act & Assert - UserId debe tener el atributo Required
        var userIdProperty = type.GetProperty(nameof(PasswordHistory.UserId));
        userIdProperty.Should().NotBeNull("La propiedad UserId no debe ser nula.");
        var userIdAttributes = userIdProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
        userIdAttributes.Should().HaveCount(1, "UserId debe tener un atributo Required.");

        // Act & Assert - PasswordHash debe tener el atributo Required
        var passwordHashProperty = type.GetProperty(nameof(PasswordHistory.PasswordHash));
        passwordHashProperty.Should().NotBeNull("La propiedad PasswordHash no debe ser nula.");
        var passwordHashAttributes = passwordHashProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
        passwordHashAttributes.Should().HaveCount(1, "PasswordHash debe tener un atributo Required.");

        // Act & Assert - PasswordChangedAt debe tener el atributo Required
        var passwordChangedAtProperty = type.GetProperty(nameof(PasswordHistory.PasswordChangedAt));
        passwordChangedAtProperty.Should().NotBeNull("La propiedad PasswordChangedAt no debe ser nula.");
        var passwordChangedAtAttributes = passwordChangedAtProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
        passwordChangedAtAttributes.Should().HaveCount(1, "PasswordChangedAt debe tener un atributo Required.");
    }

    #endregion

    #region Pruebas de integración
    [Fact]
    public void CicloDeVidaPasswordHistory_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var user = CreateValidUser();
        var originalHash = GetValidPasswordHash();
        var newHash = "$2a$10$updated.password.hash.for.user";

        // Act - Crear el historial de contraseña inicial
        var initialHistory = PasswordHistory.Create(user.Id, originalHash);

        // Assert - Estado inicial
        initialHistory.UserId.Should().Be(user.Id, "El UserId debe coincidir con el del usuario.");
        initialHistory.PasswordHash.Should().Be(originalHash, "El hash inicial debe coincidir.");
        initialHistory.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1), "La marca de tiempo inicial debe ser cercana a la actual.");

        // Act - Simular el cambio de contraseña creando una nueva entrada en el historial
        var updatedHistory = PasswordHistory.Create(user.Id, newHash);

        // Assert - Estado actualizado
        updatedHistory.UserId.Should().Be(user.Id, "El UserId debe coincidir en la entrada actualizada.");
        updatedHistory.PasswordHash.Should().Be(newHash, "El nuevo hash debe coincidir.");
        updatedHistory.PasswordHash.Should().NotBe(originalHash, "El nuevo hash no debe ser igual al original.");
        updatedHistory.PasswordChangedAt.Should().BeAfter(initialHistory.PasswordChangedAt, "La marca de tiempo actualizada debe ser posterior a la inicial.");

        // Act - Configurar la propiedad de navegación
        initialHistory.User = user;
        updatedHistory.User = user;

        // Assert - Propiedades de navegación
        initialHistory.User.Should().Be(user, "El usuario en la historia inicial debe coincidir.");
        updatedHistory.User.Should().Be(user, "El usuario en la historia actualizada debe coincidir.");
        initialHistory.User.Id.Should().Be(initialHistory.UserId, "El Id del usuario debe coincidir con el UserId en la historia inicial.");
        updatedHistory.User.Id.Should().Be(updatedHistory.UserId, "El Id del usuario debe coincidir con el UserId en la historia actualizada.");
    }

    [Fact]
    public void MultiplesEntradasDePasswordHistory_DeberianRastrearCambiosDeContraseña()
    {
        // Arrange
        var user = CreateValidUser();
        var passwords = new[]
        {
            "$2a$10$password1.hash",
            "$2a$10$password2.hash",
            "$2a$10$password3.hash"
        };

        // Act - Crear múltiples entradas en el historial de contraseñas
        var historyEntries = new List<PasswordHistory>();
        foreach (var password in passwords)
        {
            // Añadir pequeño retraso para asegurar marcas de tiempo diferentes
            Thread.Sleep(1);
            var history = PasswordHistory.Create(user.Id, password);
            historyEntries.Add(history);
        }

        // Assert - Todas las entradas deben tener marcas de tiempo y hashes diferentes
        historyEntries.Should().HaveCount(3, "Debe haber 3 entradas en el historial.");
        historyEntries.Should().OnlyContain(h => h.UserId == user.Id, "Todas las entradas deben ser del mismo usuario.");
        
        // Cada entrada debe tener un hash único
        var hashes = historyEntries.Select(h => h.PasswordHash).ToList();
        hashes.Should().OnlyHaveUniqueItems("Cada entrada debe tener un hash único.");
        
        // Las marcas de tiempo deben ser en orden ascendente (las entradas más recientes tienen marcas de tiempo más tardías)
        for (int i = 1; i < historyEntries.Count; i++)
        {
            historyEntries[i].PasswordChangedAt.Should().BeAfter(historyEntries[i - 1].PasswordChangedAt, "Las marcas de tiempo deben ser ascendentes.");
        }
    }

    [Fact]
    public void PasswordHistory_ConEscenarioDeUsuarioReal_DeberiaFuncionarCorrectamente()
    {
        // Arrange
        var user = CreateValidUser();
        var oldPasswordHash = user.PasswordHash; // Contraseña actual del usuario
        var newPasswordHash = "$2a$10$new.secure.password.hash.for.user";

        // Act - Crear el historial de contraseña antes del cambio de contraseña
        var passwordHistory = PasswordHistory.Create(user.Id, oldPasswordHash);
        passwordHistory.User = user;

        // Simular la actualización de la contraseña del usuario
        user.PasswordHash = newPasswordHash;

        // Assert - El historial de contraseña conserva la contraseña antigua
        passwordHistory.UserId.Should().Be(user.Id, "El UserId debe coincidir con el del usuario.");
        passwordHistory.PasswordHash.Should().Be(oldPasswordHash, "El hash en el historial debe ser el antiguo.");
        passwordHistory.PasswordHash.Should().NotBe(user.PasswordHash, "El hash en el historial no debe ser el actual del usuario."); // Different from current password
        passwordHistory.User.Should().Be(user, "El usuario de navegación debe coincidir.");
        passwordHistory.User.PasswordHash.Should().Be(newPasswordHash, "El hash de la contraseña del usuario debe ser el nuevo.");
        passwordHistory.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "La marca de tiempo debe ser cercana a la actual.");
    }

    [Theory]
    [InlineData(1)] // 1 día atrás
    [InlineData(7)] // 1 semana atrás  
    [InlineData(30)] // 1 mes atrás
    [InlineData(90)] // 3 meses atrás
    public void PasswordHistory_ConDiferentesAntiguedades_DeberiaRastrearCorrectamente(int daysAgo)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();
        var changeDate = DateTime.UtcNow.AddDays(-daysAgo);

        // Act
        var passwordHistory = PasswordHistory.Create(userId, passwordHash);
        passwordHistory.PasswordChangedAt = changeDate; // Simular cambio histórico

        // Assert
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir.");
        passwordHistory.PasswordHash.Should().Be(passwordHash, "El PasswordHash debe coincidir.");
        passwordHistory.PasswordChangedAt.Should().Be(changeDate, "La fecha de cambio debe coincidir con la fecha simulada.");
        passwordHistory.PasswordChangedAt.Should().BeBefore(DateTime.UtcNow, "La fecha de cambio debe ser anterior a la actual.");
        
        // Calcular la antigüedad
        var age = DateTime.UtcNow - passwordHistory.PasswordChangedAt;
        age.Days.Should().BeGreaterOrEqualTo(daysAgo - 1, "La antigüedad de la contraseña debe ser al menos el número de días especificado menos uno."); // Permitir pequeñas diferencias de tiempo
    }

    #endregion

    #region Pruebas de casos extremos
    [Fact]
    public void PasswordHistory_ConPasswordHashLargo_DeberiaManejarCorrectamente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var longPasswordHash = new string('x', 1000); // Hash muy largo

        // Act
        var passwordHistory = PasswordHistory.Create(userId, longPasswordHash);

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir.");
        passwordHistory.PasswordHash.Should().Be(longPasswordHash, "El PasswordHash largo debe coincidir.");
        passwordHistory.PasswordHash.Length.Should().Be(1000, "La longitud del hash debe ser 1000.");
    }

    [Fact]
    public void PasswordHistory_ConCaracteresEspecialesEnHash_DeberiaManejarCorrectamente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var specialCharHash = "$2a$10$ABC.123/xyz+special=chars|in~password#hash@domain!";

        // Act
        var passwordHistory = PasswordHistory.Create(userId, specialCharHash);

        // Assert
        passwordHistory.Should().NotBeNull("El objeto PasswordHistory no debería ser nulo.");
        passwordHistory.UserId.Should().Be(userId, "El UserId debe coincidir.");
        passwordHistory.PasswordHash.Should().Be(specialCharHash, "El hash con caracteres especiales debe coincidir.");
    }

    [Fact]
    public void PasswordHistory_CreadoAlMismoTiempo_DeberiaTenerIdsDiferentes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = GetValidPasswordHash();

        // Act - Crear dos historiales de contraseñas lo más rápido posible
        var history1 = PasswordHistory.Create(userId, passwordHash);
        var history2 = PasswordHistory.Create(userId, passwordHash);

        // Simular que EF Core genera IDs diferentes
        history1.Id = Guid.NewGuid();
        history2.Id = Guid.NewGuid();

        // Assert - Deberían tener IDs diferentes incluso si se crean casi al mismo tiempo
        history1.Id.Should().NotBe(history2.Id, "Los IDs deben ser diferentes incluso si se crean al mismo tiempo.");
        history1.UserId.Should().Be(history2.UserId, "Los UserIds deben ser iguales.");
        history1.PasswordHash.Should().Be(history2.PasswordHash, "Los PasswordHashes deben ser iguales.");
        // Las marcas de tiempo pueden ser las mismas o muy
    }

    #endregion
}