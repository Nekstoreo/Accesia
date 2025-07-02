---
description: "Guía de arquitectura del proyecto Accesia"
applyTo: "**"
---

# Guía de Arquitectura - Accesia

## Estructura del Proyecto

**Accesia** es un sistema de autenticación empresarial construido con **.NET 9** siguiendo los principios de **Clean Architecture**.

### Capas Arquitectónicas

#### 🎯 **API Layer** - `Accesia.API`
- **Punto de entrada**: `Program.cs`
- **Controladores**: En carpeta `Controllers/`
- **Configuración**: JWT, CORS, Swagger, Health Checks
- **Middlewares**: Autenticación, logging con Serilog

#### 🧠 **Application Layer** - `Accesia.Application`
- **Commands/Queries**: En carpeta `Features/` (patrón CQRS)
- **DTOs**: Objetos de transferencia de datos
- **Interfaces**: En carpeta `Common/Interfaces/`
- **Validadores**: FluentValidation
- **Excepciones**: En carpeta `Common/Exceptions/`

#### 🏛️ **Domain Layer** - `Accesia.Domain`
- **Entidades**: En carpeta `Entities/` (User, Role, Permission, Session)
- **Value Objects**: En carpeta `ValueObjects/` (Email, Password, DeviceInfo)
- **Enums**: En carpeta `Enums/`
- **Constantes**: En carpeta `Constants/`

#### 🔧 **Infrastructure Layer** - `Accesia.Infrastructure`
- **DbContext**: `Data/ApplicationDbContext.cs`
- **Configuraciones EF**: En carpeta `Data/Configurations/`
- **Servicios**: En carpeta `Services/`
- **Migraciones**: En carpeta `Migrations/`

## Principios Aplicados

- **Clean Architecture**: Separación clara de responsabilidades
- **Domain-Driven Design**: Entidades ricas, Value Objects
- **CQRS**: Commands y Queries separados
- **Repository Pattern**: Abstracción de acceso a datos
- **Event-Driven Architecture**: Para desacoplamiento futuro
- **SOLID Principles**: En todo el diseño

## Tecnologías Clave

- **.NET 9.0** como framework principal
- **Entity Framework Core** para persistencia
- **MediatR** para implementación de CQRS
- **FluentValidation** para validación de inputs
- **Serilog** para logging estructurado
- **JWT** para autenticación basada en tokens
- **Swagger** para documentación API
