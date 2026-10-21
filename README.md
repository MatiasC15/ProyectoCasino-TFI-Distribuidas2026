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
- **Ruleta europea en tiempo real:** Mesa compartida, apuestas autenticadas, liquidación de premios y notificaciones por SignalR/WebSocket.

## Ruleta

La API inicia rondas compartidas automáticamente. La ruleta usa un único cero y admite apuestas a número (0–36), color, paridad, docena, apuestas exteriores, divisiones y esquinas. El intervalo entre rondas, las fichas disponibles y los límites de apuesta se administran desde `/admin` y se guardan en PostgreSQL.

Todos los endpoints requieren JWT:

- `GET /api/roulette/state`: devuelve la última ronda y su estado.
- `GET /api/roulette/settings`: límites y valores de ficha que usa actualmente la mesa.
- `POST /api/roulette/bets`: apuesta en la ronda indicada. `requestId` debe ser un UUID nuevo para cada apuesta y debe reutilizarse si el cliente reintenta esa misma solicitud.
- `wss://<host>/hubs/roulette?access_token=<JWT>`: conexión SignalR. El servidor envía `roulette-event` con eventos `round-opened`, `round-settled`, `bet-accepted` y `round-state`.

Ejemplo de apuesta a un número:

```json
{
  "requestId": "7a8f1ce7-0492-4fe0-b7d5-b725c9b53f3a",
  "roundId": "<id devuelto por /api/roulette/state>",
  "amount": 100.00,
  "type": "number",
  "selection": "17"
}
```

El saldo se debita en PostgreSQL dentro de la misma transacción que registra la apuesta; el cierre de ronda bloquea la ronda hasta liquidar todas las apuestas, y una restricción única impide dos rondas abiertas. Los créditos de premios y sus transacciones se confirman junto con el resultado. Las operaciones de depósito y retiro también usan actualizaciones atómicas de saldo para no pisar apuestas concurrentes.

## Administración

Los roles se guardan en el JWT como `user`, `admin` y `superadmin`. La ruta `/admin` es solo una barrera de interfaz; los endpoints administrativos vuelven a validar el rol en el backend. Un usuario que no sea admin vuelve a la última página de la aplicación que visitó. Los administradores pueden revisar actividad, estadísticas, rondas recientes y el registro de cambios, además de configurar el intervalo entre tiradas (5–300 segundos), los montos mínimo/máximo y de 1 a 12 fichas únicas. Los cambios de intervalo se aplican a la próxima ronda; los cambios de límites se validan para cada apuesta nueva. Las actualizaciones de configuración se serializan con bloqueos de PostgreSQL para mantener coherencia con apuestas concurrentes.

La cuenta inicial de superadmin se crea en el primer arranque de la API que encuentre estos valores configurados. No hay credenciales predeterminadas en el repositorio. En PowerShell, establecé valores propios para el proceso que va a iniciar la API:

```powershell
$env:AdminBootstrap__Email = "<correo del superadmin>"
$env:AdminBootstrap__Dni = "<DNI de 7 u 8 dígitos>"
$env:AdminBootstrap__Nombre = "<nombre>"
$env:AdminBootstrap__Apellido = "<apellido>"
$env:AdminBootstrap__FechaNacimiento = "<YYYY-MM-DD>"
$env:AdminBootstrap__Password = "<contraseña de al menos 12 caracteres>"
dotnet run --project Casino.API
```

La cuenta inicial se crea una sola vez. Un superadmin puede crear otras cuentas admin desde el panel; los admins comunes no pueden otorgar roles. Cada creación de administrador y cada cambio de reglas queda asentado en `AdminAuditLogs`. Si las variables de arranque no están configuradas, la aplicación no crea una cuenta privilegiada.

##  Instalación y Uso
1. Clonar el repositorio.
2. Configurar la cadena de conexión a PostgreSQL en `Casino.API/appsettings.json`.
3. Configurar la cuenta inicial de superadmin con las variables de entorno de la sección Administración.
4. Iniciar la API con `dotnet run --project Casino.API`; el arranque aplica las migraciones pendientes.
5. Acceder a `https://localhost:<puerto>/swagger` para probar los endpoints.

## Pruebas automatizadas

Ejecutá `dotnet test Casino.Tests/Casino.Tests.csproj`. La suite contiene pruebas unitarias de reglas y pruebas de integración de login, depósito, migraciones y concurrencia sobre una base PostgreSQL temporal local que elimina al finalizar. Requiere permiso `CREATEDB`; no utiliza la base `CasinoDB`. El detalle de resultados, errores corregidos y revisión de seguridad está en [docs/PRUEBAS_Y_SEGURIDAD.md](docs/PRUEBAS_Y_SEGURIDAD.md).
