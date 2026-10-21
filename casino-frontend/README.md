# Casino Club — Frontend

Aplicación React + JavaScript para el casino académico. Mantiene una separación práctica por capas: `domain` contiene utilidades de negocio, `application/useCases` orquesta acciones, `infrastructure` integra HTTP/SignalR y `presentation` contiene páginas y estado visual.

## Funciones

- Inicio de sesión JWT.
- Billetera de créditos de práctica: consulta de saldo, carga y retiro.
- Catálogo de juegos activos.
- Ruleta europea de un solo cero, compartida y actualizada con SignalR.
- Rueda europea 3D hecha con Three.js: la bola desacelera y queda en la casilla del número decidido por el servidor; se destaca el resultado y el resumen de apuestas ganadas/perdidas de esta sesión.
- Apuestas a número, color, paridad y docena. Cada intento usa un `requestId` idempotente.
- Diseño adaptable a escritorio y móvil.

La geometría de la rueda se genera localmente con Three.js; no se descargó un modelo 3D ni un video externo.

## Ejecución local en VS Code

1. Abrí la carpeta raíz del repositorio en VS Code.
2. En una terminal, iniciá la API desde el directorio raíz:

   ```powershell
   dotnet run --project Casino.API --launch-profile https
   ```

3. En otra terminal, iniciá la interfaz:

   ```powershell
   cd casino-frontend
   npm install
   npm run dev
   ```

4. Abrí `http://localhost:5173`.

Vite usa un proxy de desarrollo a `https://localhost:7298` para `/api` y `/hubs`; `secure: false` aplica solo al proxy local para permitir el certificado de desarrollo. Para otro puerto se puede cambiar `api` en `vite.config.js`. En producción, configurar `VITE_API_BASE_URL` con el origen HTTPS público de la API y `Frontend__AllowedOrigins__0` con el origen HTTPS del frontend.

## API que consume

| Método | Ruta | Uso |
|---|---|---|
| `POST` | `/api/auth/login` | Iniciar sesión |
| `GET` | `/api/games` | Juegos activos |
| `GET` | `/api/wallet/balance` | Saldo autenticado |
| `POST` | `/api/wallet/deposit` | Cargar créditos (`{ "amount": 100 }`) |
| `POST` | `/api/wallet/withdraw` | Retirar créditos (`{ "amount": 50 }`) |
| `GET` | `/api/roulette/state` | Estado de la ronda |
| `POST` | `/api/roulette/bets` | Registrar apuesta |
| SignalR | `/hubs/roulette` | Eventos compartidos de ronda |

`GET /api/wallet/balance` y `GET /api/games` fueron añadidos al backend para dar soporte a la interfaz.

## Consideraciones de seguridad y concurrencia

- El frontend nunca decide el resultado de la rueda, el premio ni el saldo final: las operaciones se validan en el servidor y en PostgreSQL.
- Los débitos de apuestas se hacen en el servidor y la base impone una clave idempotente única por usuario. El cliente genera un `requestId` para que un reintento de apuesta no vuelva a descontar.
- SignalR reconecta la conexión. La difusión actual usa memoria local y sirve para una instancia del backend; antes de ejecutar varias réplicas, agregar un backplane/pub-sub compartido (por ejemplo, Redis) para que todos reciban los mismos eventos.
- Los botones bloquean envíos mientras la operación está pendiente. Los movimientos de billetera aún no reciben una clave idempotente: antes de conectar pagos reales, agregar idempotencia y reconciliación entre el proveedor y la base de datos.
- El contador solo reemplaza su ronda con eventos de estado (`round-state`, `round-opened`, `round-settled`). El acuse `bet-accepted` no lleva fecha de cierre y se ignora para el reloj; fechas no válidas muestran `--:--` en lugar de `NaN`.
- La sesión se guarda en `sessionStorage` para limitar su duración al contexto de la pestaña. Sigue siendo accesible para JavaScript; un XSS podría robar el token. En una publicación real conviene migrar a cookies `HttpOnly`, `Secure`, `SameSite`, junto con protección CSRF y una política CSP.
- Depósitos y retiros son movimientos simulados del proyecto universitario. No conectan con medios de pago ni representan dinero real.
- El proxy Vite desactiva la validación del certificado únicamente en desarrollo. No copiar esa opción a un proxy de producción.

## Compilación

```powershell
npm run build
```

Ver [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md) para la licencia de Three.js.
