<h1 align="center">🔐 Accesia</h1>
<p align="center"><strong>Sistema Empresarial de Autenticación y Gestión de Identidades</strong></p>

<p align="center">
    <img src="https://img.shields.io/badge/.NET-9.0-blue.svg" alt=".NET 9.0"/>
    <img src="https://img.shields.io/badge/PostgreSQL-16-blue.svg" alt="PostgreSQL 16"/>
    <img src="https://img.shields.io/badge/Arquitectura-Clean-green.svg" alt="Arquitectura Limpia"/>
    <img src="https://img.shields.io/badge/Estado-Desarrollo-yellow.svg" alt="Estado del Proyecto"/>
</p>

<p align="center">
    Accesia es un sistema de autenticación empresarial robusto, seguro y altamente configurable, diseñado para proporcionar una solución integral de gestión de identidades y accesos.
</p>

## 🚀 Características Principales

### 1. Registro y Autenticación Segura
- Registro con verificación de email mediante tokens únicos
- Proceso de login seguro con JWT y refresh tokens
- Bloqueo inteligente por intentos fallidos de acceso
- Rate limiting para prevenir ataques de fuerza bruta

### 2. Gestión Avanzada de Usuarios
- Perfiles de usuario completamente personalizables
- Sistema de roles y permisos granular y jerárquico
- Control de estados de cuenta (activo, inactivo, bloqueado)
- Historial de cambios y auditoría de cuenta

### 3. Seguridad de Contraseñas
- Hashing de contraseñas con BCrypt
- Validación robusta de complejidad de contraseñas
- Restablecimiento seguro de contraseña
- Historial de contraseñas para prevenir reutilización

### 4. Control de Acceso Basado en Roles (RBAC)
- Roles y permisos completamente personalizables
- Jerarquías de roles con herencia de permisos
- Asignación dinámica de roles
- Permisos contextuales por departamento

### 5. Autenticación Multifactor
- Verificación de dos factores por correo electrónico
- Códigos de recuperación
- Notificaciones de acceso desde nuevos dispositivos

### 6. Gestión de Sesiones y Dispositivos
- Identificación y seguimiento de dispositivos
- Cierre de sesión remoto
- Límite de sesiones concurrentes
- Registro detallado de actividad de sesiones

### 7. Administración del Sistema
- Panel de administración completo
- Configuración global del sistema
- Monitoreo y análisis en tiempo real
- Generación de reportes de actividad

### 8. Seguridad y Cumplimiento
- Protección contra ataques comunes (SQL Injection, XSS, CSRF)
- Logging estructurado y auditoría de seguridad
- Cumplimiento con mejores prácticas de seguridad
- Configuraciones de seguridad altamente personalizables

## 🛠 Tecnologías

- **Backend**: .NET 9.0
- **Base de Datos**: PostgreSQL 16
- **Contenedorización**: Docker Compose
- **Autenticación**: JWT, Two-Factor Authentication
- **Seguridad**: BCrypt, Rate Limiting
- **Logging**: Serilog
- **Validación**: FluentValidation
- **Arquitectura**: Clean Architecture, CQRS

## 📦 Instalación Rápida

### Requisitos Previos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- [Docker & Docker Compose](https://docs.docker.com/compose/install/)

### Configuración de Base de Datos

Accesia utiliza PostgreSQL 16 como base de datos principal. La configuración de Docker Compose facilita el despliegue:

```yaml
services:
  db:
    image: postgres:16
    container_name: accesia-db
    environment:
      - POSTGRES_DB=accesia_dev
      - POSTGRES_USER=accesia_user
      - POSTGRES_PASSWORD=mysecretpassword
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
```

### Pasos de Instalación

1. **Clonar Repositorio**
   ```bash
   git clone https://github.com/Nekstoreo/Accesia.git
   cd Accesia
   ```

2. **Configurar Variables de Entorno**
   Copiar y configurar `appsettings.Development.example.json` a `appsettings.Development.json`, 
   asegurándose de que los parámetros de conexión coincidan con la configuración de Docker Compose:

   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=localhost;Port=5432;Database=accesia_dev;Username=accesia_user;Password=mysecretpassword"
     }
   }
   ```

3. **Levantar Base de Datos**
   ```bash
   docker-compose up -d db
   ```

4. **Crear Base de Datos**
   ```bash
   dotnet ef database update
   ```

5. **Ejecutar Aplicación**
   ```bash
   dotnet run --project Accesia.API
   ```

### Ejecución Completa con Docker

Para levantar toda la infraestructura:

```bash
docker-compose up --build
```

> **Nota:** Docker Compose gestiona automáticamente la base de datos PostgreSQL, facilitando el despliegue y la configuración del entorno de desarrollo.

## 🔒 Seguridad

Accesia implementa múltiples capas de seguridad:
- Hashing de contraseñas con salt único
- Tokens JWT con claims específicos
- Validación exhaustiva de entradas
- Protección contra ataques comunes
- Logging de seguridad detallado

## 🤝 Contribuciones

¡Las contribuciones son bienvenidas! Por favor, lee nuestras [Guías de Contribución](CONTRIBUTING.md) antes de comenzar.

## 📄 Licencia

Este proyecto está bajo [LICENCIA]. Consulta el archivo `LICENSE` para más detalles.

## 📧 Contacto

**Autor**: Néstor Gutiérrez
- GitHub: [@Nekstoreo](https://github.com/Nekstoreo)
- Email: nestorg456k@outlook.com

---

<p align="center">
    <strong>Accesia</strong> - Autenticación Empresarial Moderna 🚀
</p>
