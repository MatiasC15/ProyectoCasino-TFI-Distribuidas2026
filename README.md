# Casino Online API - Arquitectura Distribuida

Backend para plataforma de casino online, desarrollado con **.NET 10** y **PostgreSQL** bajo los principios de **Clean Architecture**.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)

## Trabajo Final Integrador
**Cátedra:** Aplicaciones Distribuidas  
**Institución:** Universidad Tecnológica Nacional - Facultad Regional Tucumán (UTN-FRT)  

## Arquitectura
El proyecto está dividido en capas para garantizar el desacoplamiento y la mantenibilidad:
- `Casino.Domain`: Entidades core del negocio (Users, Bets, Games).
- `Casino.Application`: Casos de uso (Use Cases), Interfaces (Contratos) y DTOs.
- `Casino.Infrastructure.Data`: Acceso a datos con EF Core, Migraciones y Repositorios.
- `Casino.Infrastructure.External`: Servicios externos (Simulador Renaper, Generador JWT).
- `Casino.API`: Controladores, inyección de dependencias y configuración de Swagger.

## Características Principales
- **Autenticación y Autorización:** Implementación de JWT Bearer tokens.
- **Validación de Identidad:** Integración (Adapter) para consumo de API Renaper (mayoría de edad).
- **Seguridad:** Hasheo de contraseñas mediante BCrypt.
- **Base de Datos:** Persistencia en PostgreSQL mediante Entity Framework Core (Code-First).

##  Instalación y Uso
1. Clonar el repositorio.
2. Configurar la cadena de conexión a PostgreSQL en `Casino.API/appsettings.json`.
3. Ejecutar `Update-Database` en la consola para impactar las migraciones.
4. Levantar la API y acceder a `https://localhost:<puerto>/swagger` para probar los endpoints.
5. No te olvides del appsetings
