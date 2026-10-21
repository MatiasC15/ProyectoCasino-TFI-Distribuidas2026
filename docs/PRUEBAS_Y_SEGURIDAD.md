# Pruebas y revisión de seguridad

Fecha de ejecución: 2026-10-02. Solución: .NET 10, PostgreSQL 18 local.

## Cómo ejecutar

Desde la raíz del repositorio:

```powershell
dotnet test Casino.Tests/Casino.Tests.csproj --logger "console;verbosity=normal"
```

Las pruebas de integración leen `Casino.API/appsettings.json`, verifican que PostgreSQL esté en `localhost`, crean una base con nombre aleatorio `casino_test_*`, aplican todas las migraciones y la eliminan al terminar. El usuario PostgreSQL configurado necesita permiso `CREATEDB`. No se modifica `CasinoDB`. La limpieza también se ejecuta al finalizar la suite.

## Resultado ejecutado

Resultado final: **5 pruebas aprobadas, 0 fallidas**. La compilación de la solución también terminó sin errores ni advertencias.

| Prueba | Tipo | Resultado |
| --- | --- | --- |
| `EuropeanRouletteRulesCoverEveryPocketAndPayout` | Unitaria | Verifica bolsillos 0–36, colores, paridad, docenas y pagos. |
| `RouletteRejectsInvalidSelectionsAmountsAndRequestIds` | Unitaria | Rechaza monto cero, más de dos decimales, selecciones inválidas y `requestId` vacío. |
| `DepositCreatesFundedTestUserAndLoginAcceptsOnlyCorrectPassword` | Integración | Crea un usuario desechable, deposita 250, valida el JWT real generado y rechaza contraseña/usuario incorrectos. El wallet es un doble de prueba exitoso. |
| `ConcurrentBetsCannotOverspendAndSettlementIsIdempotent` | Integración/concurrencia | Dos apuestas simultáneas de 75 con saldo 100: confirma una sola; un reintento igual no duplica, reutilizar la clave con otros datos y apostar tras el cierre se rechazan; liquidar dos veces no duplica premios. |
| `EfSnapshotMatchesCurrentModel` | Integración/migraciones | Compara el modelo EF actual con el snapshot de migraciones. La prueba de integración también aplica todas las migraciones a PostgreSQL limpio. |

El usuario, el depósito, la apuesta, el resultado y los movimientos de prueba existen solo en la base desechable. El resultado de la ruleta es aleatorio: la prueba comprueba el pago que corresponde al número que salió, sin depender de un resultado predeterminado.

## Errores encontrados y corregidos

1. La primera compilación falló porque el evento `bet-accepted` definía dos campos anónimos con el nombre `Id`. Se renombraron a `BetId` y `RoundId`.
2. La primera inicialización de PostgreSQL falló con `PendingModelChangesWarning`: el snapshot conservaba `IX_Bets_UserId`, índice redundante al existir el índice compuesto por usuario e idempotencia. Se actualizó el snapshot y la migración ahora elimina ese índice; la reversa lo vuelve a crear.
3. La primera compilación del proyecto de pruebas usó aserciones retiradas de MSTest 4 y no tenía referencia a `ConfigurationBuilder`. Se cambiaron las aserciones y se agregaron las dependencias explícitas.
4. Después de esas correcciones, la suite completa aplicó migraciones en PostgreSQL local y terminó con 5/5 pruebas aprobadas.

## Vulnerabilidades y límites que quedan

- **Credenciales en configuración:** `appsettings.json` contiene una contraseña de base y una clave JWT en texto plano. Usar User Secrets o un gestor de secretos; si esos valores llegaron a Git remoto, rotarlos.
- **Wallet y RENAPER simulados:** el wallet actual siempre responde éxito y RENAPER aprueba a todos. No habilitar depósitos, retiros ni registro real hasta conectar y validar esos servicios.
- **Operaciones distribuidas de dinero:** el depósito acredita localmente después de llamar al wallet; un fallo de base puede dejar desincronizados ambos saldos. En retiros, un timeout externo puede ser ambiguo. Faltan claves de idempotencia compartidas, ledger completo y compensación/outbox para producción.
- **Fuerza bruta de login:** no hay limitación de intentos ni bloqueo temporal; la contraseña mínima actual es corta para una cuenta con saldo.
- **JWT no revocable:** la API emite tokens firmados con vencimiento, pero no implementa refresh/revocación ante robo. La prueba valida emisión y claims; no prueba una negociación HTTP completa ni la recepción del hub SignalR.
- **Filtración de tokens SignalR:** el JWT se acepta mediante `access_token` en la URL del hub, convención de SignalR para WebSocket. Usar HTTPS y configurar proxies/logs para no registrar query strings.
- **Auditoría de imparcialidad:** el número se genera con un generador criptográfico seguro, pero no hay un mecanismo público de compromiso/verificación. Si la transacción de liquidación falla, el reintento puede generar otro número antes de publicar un resultado.
- **Varias instancias:** rondas y saldos se coordinan en PostgreSQL, pero el canal de eventos y el envío SignalR viven en memoria en una instancia. Para escalar se necesita un backplane (por ejemplo Redis) y entrega durable de eventos.
- **Enumeración en registro:** los errores de registro distinguen DNI y email ya existentes. El login sí devuelve el mismo mensaje para usuario inexistente y contraseña incorrecta.

Esta revisión es técnica y acotada al código presente; no reemplaza una auditoría de seguridad completa ni una prueba de carga.
