---
description: "Instrucciones para pruebas automatizadas en el proyecto Accesia"
applyTo: "Accesia.Tests/**"
---

# Estándares de Testing

## Organización de Tests

### Estructura de Carpetas
Los tests deben organizarse en un único proyecto `Accesia.Tests` con subdirectorios para tests unitarios e integración, siguiendo la misma estructura que el código fuente:

```
Accesia.Tests/
  ├── UnitTests/
  │   ├── Application/
  │   │   └── Features/
  │   │       ├── Authentication/
  │   │       │   ├── Commands/
  │   │       │   │   ├── LoginUserCommandTests.cs
  │   │       │   │   └── ...
  │   │       │   └── Queries/
  │   │       └── Users/
  │   │           ├── Commands/
  │   │           └── Queries/
  │   ├── Domain/
  │   │   ├── Entities/
  │   │   │   └── UserTests.cs
  │   │   └── ValueObjects/
  │   │       ├── EmailTests.cs
  │   │       └── PasswordTests.cs
  │   └── Infrastructure/
  │       └── Services/
  │           ├── JwtTokenServiceTests.cs
  │           └── ...
  │
  ├── IntegrationTests/
  │   ├── API/
  │   │   └── Controllers/
  │   │       ├── AuthControllerTests.cs
  │   │       └── ...
  │   ├── Application/
  │   │   └── Features/
  │   │       └── End2EndTests/
  │   ├── Infrastructure/
  │   │   ├── Repositories/
  │   │   └── Data/
  │   │       └── ApplicationDbContextTests.cs
  │   └── TestBase/
  │       ├── IntegrationTestFixture.cs
  │       ├── TestWebApplicationFactory.cs
  │       └── Utilities/
  │
  └── TestHelpers/
      ├── Fixtures/
      ├── Mocks/
      └── TestData/
```

### Convención de Nombres
- **Clase**: `{ClaseATestear}Tests`
- **Método**: `{MétodoATestear}_{ResultadoEsperado}_{Condición}`
  - Ejemplo: `Login_ShouldReturnToken_WhenCredentialsAreValid`

## Tipos de Tests

### Tests Unitarios (Accesia.Tests/UnitTests)
- Testean una unidad de funcionalidad aislada
- Uso de mocks para todas las dependencias externas
- Rápidos de ejecutar (milisegundos)
- No acceden a base de datos, sistema de archivos o red
- Cubren casos de éxito y casos de error
- Framework: xUnit

### Tests de Integración (Accesia.Tests/IntegrationTests)
- Testean la interacción entre múltiples componentes
- Pueden incluir base de datos en memoria o containers Docker
- Más lentos que los unitarios pero más completos
- Validan el comportamiento real del sistema
- Framework: xUnit con WebApplicationFactory

#### Tests de API
- Subconjunto de tests de integración enfocados en endpoints
- Validación de respuestas HTTP, headers y cuerpo
- Verifican el pipeline completo de la aplicación
- Pueden usar TestServer o HttpClient

#### Tests de Repositorios
- Prueban el acceso a datos con una base de datos real o en memoria
- Verifican mappings, queries y operaciones CRUD
- Utilizan migrations para crear el esquema

## Patrón AAA (Arrange-Act-Assert)

Todos los tests deben seguir el patrón AAA:

```csharp
[Fact]
public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
{
    // Arrange
    var command = new LoginUserCommand
    {
        Email = "test@example.com",
        Password = "ValidPassword123!"
    };
    
    var mockUserRepository = new Mock<IUserRepository>();
    // Configurar mocks...
    
    var handler = new LoginUserHandler(
        mockUserRepository.Object,
        /* otras dependencias */
    );
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.Token);
    Assert.NotEmpty(result.RefreshToken);
    // Otras aserciones...
}
```

## Datos de Test

### Datos Estáticos
Usar datos constantes para casos simples:

```csharp
private static readonly User ValidUser = new()
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
    Email = "test@example.com",
    // Otras propiedades...
};
```

### Data-Driven Tests
Para probar múltiples casos similares:

```csharp
[Theory]
[InlineData("", "password", "El email es requerido")]
[InlineData("invalid", "password", "Email inválido")]
[InlineData("test@example.com", "", "La contraseña es requerida")]
public async Task Validate_ShouldReturnErrors_WhenDataIsInvalid(
    string email, string password, string expectedError)
{
    // Arrange
    var command = new LoginUserCommand { Email = email, Password = password };
    var validator = new LoginUserCommandValidator();
    
    // Act
    var result = await validator.ValidateAsync(command);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedError));
}
```

## Mocking

### Configuración de Mocks
Usar Moq para crear mocks de dependencias:

```csharp
var mockUserRepository = new Mock<IUserRepository>();
mockUserRepository
    .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(user);
```

### Verificación de Llamadas
Verificar que los métodos se llamaron correctamente:

```csharp
mockUserRepository.Verify(
    r => r.GetByEmailAsync(It.Is<string>(e => e == command.Email), It.IsAny<CancellationToken>()),
    Times.Once);
```

## Buenas Prácticas

### Principios Generales
1. **Independencia**: Cada test debe poder ejecutarse de forma aislada
2. **Determinismo**: El resultado debe ser el mismo en cada ejecución
3. **Rapidez**: Los tests deben ejecutarse rápidamente
4. **Claridad**: El propósito del test debe ser claro
5. **Cobertura**: Testear casos positivos y negativos

### Para Unit Tests
- Mantener el enfoque en probar una sola unidad funcional
- No cruzar límites de procesos o componentes (no DB, no APIs)
- Mockar todas las dependencias externas
- Centrados en la lógica de negocio, no en la infraestructura
- Verificar únicamente comportamientos públicos, no detalles de implementación

### Para Integration Tests
- Usar datos lo más cercanos posible a la realidad
- Establecer un estado conocido antes de cada test
- Limpiar recursos después de cada test
- Considerar el uso de containers para servicios externos
- Paralelizar ejecución cuando sea posible (con cuidado de no crear interferencias)

### Anti-patrones a Evitar
- Tests que dependen de otros tests
- Tests con lógica condicional compleja
- Tests con múltiples aserciones no relacionadas
- Dependencia de recursos externos sin mockear o contenerizar
- Tests frágiles que fallan por razones no relacionadas con lo que se prueba

## Herramientas Recomendadas

### Frameworks y Librerías
- **xUnit**: Framework de testing principal
- **Moq**: Librería de mocking para unit tests
- **FluentAssertions**: Aserciones más legibles y expresivas
- **Bogus**: Generación de datos aleatorios para pruebas
- **AutoFixture**: Creación automática de objetos de test
- **Respawn**: Limpieza de base de datos entre tests
- **Testcontainers**: Contenedores Docker para servicios externos
- **WebApplicationFactory**: Servidor de prueba para APIs
- **Microsoft.EntityFrameworkCore.InMemory**: DB en memoria para tests

## Ejecución de Tests

### Ejecución por Tipo
Para facilitar la ejecución selectiva de los tests según su tipo, se recomienda usar las etiquetas de categoría de xUnit:

```csharp
// Para tests unitarios
[Trait("Category", "Unit")]
public class SomeUnitTest
{
    // ...
}

// Para tests de integración
[Trait("Category", "Integration")]
public class SomeIntegrationTest
{
    // ...
}
```

Esto permite ejecutar selectivamente:

```bash
# Ejecutar solo tests unitarios
dotnet test --filter Category=Unit

# Ejecutar solo tests de integración
dotnet test --filter Category=Integration

# Ejecutar todos los tests
dotnet test
```
