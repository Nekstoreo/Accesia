---
description: "Estándares para la escritura de código C# en el proyecto Accesia"
applyTo: "**/*.cs"
---

# Estándares de Código C#

## Estilo y Formato

### Nomenclatura
- **Clases, Interfaces, Records**: PascalCase (ej: `UserService`, `IUserRepository`)
- **Métodos**: PascalCase (ej: `GetUserById`, `CreateNewUser`)
- **Variables locales**: camelCase (ej: `userName`, `userList`)
- **Parámetros**: camelCase (ej: `userId`, `updateRequest`)
- **Propiedades públicas**: PascalCase (ej: `FirstName`, `EmailAddress`)
- **Campos privados**: `_camelCase` (ej: `_userRepository`, `_logger`)
- **Constantes**: UPPER_CASE (ej: `MAX_LOGIN_ATTEMPTS`, `DEFAULT_TIMEOUT`)

### Organización del Código
- Usar directivas `#region` para agrupar código relacionado
- Orden de elementos: campos, constructores, propiedades, métodos
- Organizar usings alfabéticamente y eliminar los no utilizados
- Separar métodos con una línea en blanco para mejorar legibilidad

### Formato
- Usar 4 espacios para indentación (no tabulaciones)
- Límite de 120 caracteres por línea
- Llaves en línea nueva para clases y métodos
- Llaves en la misma línea para propiedades, lambdas y expresiones cortas

## Buenas Prácticas

### Inmutabilidad
- Preferir `record` para DTOs y objetos de valor
- Usar `readonly` para campos que no cambian después de la inicialización
- Considerar tipos inmutables para modelos de dominio

### Null Safety
- Usar anotaciones de nullabilidad (`string?`, `int?`)
- Validar parámetros no nulos al inicio de los métodos
- Utilizar el operador `??` y `?.` para manejo seguro de nulos

### Asincronía
- Usar `async/await` consistentemente, no mezclar con código sincrónico
- Incluir sufijo `Async` para métodos asincrónicos
- Pasar `CancellationToken` a métodos asincrónicos
- Evitar `.Result` y `.Wait()` para prevenir deadlocks

### LINQ
- Preferir sintaxis de método sobre sintaxis de consulta
- Encadenar operaciones para consultas complejas
- Extraer consultas complejas a métodos con nombres descriptivos
- Usar `ToListAsync()`, `FirstOrDefaultAsync()`, etc. para operaciones asincrónicas

## Características Modernas de C#

### Expresiones Switch y Pattern Matching
```csharp
var result = user.Status switch
{
    UserStatus.Active => ProcessActiveUser(user),
    UserStatus.Suspended => HandleSuspendedUser(user),
    UserStatus.Deleted => throw new UserDeletedException(user.Id),
    _ => throw new ArgumentOutOfRangeException(nameof(user.Status))
};
```

### Records para DTOs
```csharp
public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    DateTime CreatedAt
);
```

### Inicializadores de propiedades
```csharp
var user = new User
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com"
};
```

### Init-only properties
```csharp
public class User
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    // ...
}
```

## Comentarios y Documentación

### Comentarios XML
- Documentar todas las APIs públicas con comentarios XML
- Incluir descripción, parámetros, valores de retorno y excepciones
- Documentar comportamiento no obvio o decisiones importantes

```csharp
/// <summary>
/// Autentica un usuario y crea una nueva sesión.
/// </summary>
/// <param name="request">Credenciales de autenticación</param>
/// <param name="ct">Token de cancelación</param>
/// <returns>Información de autenticación incluyendo tokens JWT</returns>
/// <exception cref="ValidationException">Si las credenciales son inválidas</exception>
/// <exception cref="UserLockedException">Si la cuenta está bloqueada</exception>
public async Task<LoginResponse> Handle(LoginUserCommand request, CancellationToken ct)
```

### Comentarios Inline
- Usar comentarios para explicar "por qué", no "qué" o "cómo"
- Comentar código complejo o que no sigue patrones estándar
- Evitar comentarios redundantes que simplemente repiten el código
