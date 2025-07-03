---
description: "Instrucciones generales para la codificación en el proyecto Accesia"
applyTo: "**"
---

# Instrucciones Generales de Codificación

## Principios Fundamentales

### Principios SOLID
1. **S**ingle Responsibility: Cada clase debe tener una única razón para cambiar
2. **O**pen/Closed: Las entidades deben estar abiertas para extensión, cerradas para modificación
3. **L**iskov Substitution: Las subclases deben poder sustituir a sus clases base
4. **I**nterface Segregation: Es mejor muchas interfaces específicas que una general
5. **D**ependency Inversion: Depender de abstracciones, no de implementaciones concretas

### Código Limpio
- **Claridad**: El código debe ser fácil de leer y entender
- **Simplicidad**: Evitar complejidad innecesaria
- **Consistencia**: Seguir convenciones y patrones uniformes
- **Modularidad**: Componentes pequeños y cohesivos
- **DRY**: Don't Repeat Yourself - Evitar duplicación de código

## Directrices Generales

### Organización del Código
- Mantener archivos pequeños (preferiblemente < 300 líneas)
- Métodos cortos y enfocados (preferiblemente < 30 líneas)
- Máximo 3-4 niveles de indentación por método

### Nombrado
- Nombres descriptivos que revelen intención
- Evitar abreviaciones excepto las muy conocidas
- Usar terminología del dominio consistentemente
- Prefijos/sufijos consistentes (Manager, Service, Repository, etc.)

### Comentarios
- Comentar el "por qué", no el "qué" o "cómo"
- Evitar comentarios que simplemente repiten el código
- Documentar APIs públicas con comentarios XML
- Marcar código problemático con `TODO:` o `FIXME:`

### Control de Versiones
- Commits pequeños y enfocados
- Mensajes descriptivos (ver commit-message.instructions.md)
- Pull requests con cambios relacionados
- Revisar código antes de hacer commit

## Seguridad

### Validación de Entrada
- Validar todas las entradas del usuario
- No confiar en datos de cliente
- Usar FluentValidation para validación de modelos
- Implementar defensas contra inyección SQL, XSS, CSRF

### Manejo de Datos Sensibles
- No almacenar secretos en código fuente
- Usar variables de entorno o Azure Key Vault
- Encriptar datos sensibles en reposo
- Logs sin información sensible

### Autenticación y Autorización
- Validar permisos en cada operación sensible
- Implementar principio de mínimo privilegio
- Usar tokens con tiempo de expiración corto
- Validación robusta de JWT

## Rendimiento

### Optimizaciones de Base de Datos
- Índices para consultas frecuentes
- Queries optimizadas (incluir solo lo necesario)
- Evitar N+1 queries usando Include() o proyecciones
- Usar paginación para conjuntos grandes de datos

### Optimizaciones de API
- Compresión de respuestas HTTP
- Uso apropiado de caché (ETags, Cache-Control)
- Respuestas asincrónicas para operaciones lentas
- Minimizar el tamaño de las respuestas

### Gestión de Recursos
- Liberar recursos con `using` o `IDisposable`
- Manejo cuidadoso de conexiones a bases de datos
- Evitar bloqueos de threads con programación asincrónica
- Rate limiting para proteger recursos escasos

## Mantenibilidad

### Registro (Logging)
- Logging estructurado con Serilog
- Niveles de log apropiados (Debug, Info, Warning, Error)
- Contexto suficiente para depuración
- Métricas para operaciones críticas

### Configuración
- Configuración externalizada (no hardcoded)
- Settings fuertemente tipados con IOptions<T>
- Validación de configuración al inicio
- Documentación de opciones de configuración

### Tolerancia a Fallos
- Manejo de excepciones apropiado
- Reintentos para operaciones transitorias
- Circuit breakers para dependencias externas
- Degradación elegante cuando servicios están caídos

### Observabilidad
- Tracing distribuido
- Métricas de rendimiento clave
- Correlación de logs entre servicios
- Monitorización de salud del sistema
