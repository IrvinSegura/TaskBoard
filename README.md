# TaskBoard

[![ci](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml)

API de tablero de tareas estilo Kanban construida con ASP.NET Core. Los equipos tienen tableros, los tableros tienen columnas y tarjetas, y cada cambio se envía en tiempo real a los clientes conectados mediante SignalR.

## Funcionalidades

- **Autenticación**: registro e inicio de sesión con tokens de acceso JWT de corta duración y refresh tokens rotativos; cierre de sesión, cambio de contraseña y `GET /me`. Límite de intentos en el login y el registro.
- **Equipos**: crear equipos, agregar y quitar miembros, cambiar roles y salir de un equipo.
- **Tableros**: tableros por equipo con miembros, roles y visibilidad; renombrarlos, archivarlos y restaurarlos. Los tableros archivados son de solo lectura.
- **Columnas y tarjetas**: crear, renombrar y reordenar columnas; crear, editar, mover, archivar y restaurar tarjetas, asignar o desasignar miembros, y buscar o filtrar las tarjetas de un tablero.
- **Comentarios y adjuntos**: agregar, editar y borrar comentarios; subir, descargar y borrar archivos (disco local o Azure Blob Storage).
- **Notificaciones**: notificaciones personales con marca de leído.
- **Tiempo real**: eventos de tablero y notificaciones enviados por SignalR.
- **Operación**: health checks (`/health`), imagen Docker, migraciones como paso de despliegue y soporte para varias instancias con Redis.

## Stack tecnológico

| Área | Tecnología |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Persistencia | EF Core 10, PostgreSQL 17 |
| Tiempo real | SignalR (con backplane de Redis cuando hay varias instancias) |
| Caché | Redis, o en memoria con una sola instancia |
| Validación | FluentValidation |
| Almacenamiento de archivos | Disco local o Azure Blob Storage (Azurite en local) |
| Logging | Serilog |
| Documentación de la API | OpenAPI + Scalar |
| Pruebas | xUnit, Shouldly, NSubstitute, Testcontainers, NetArchTest |

## Arquitectura

La solución sigue Clean Architecture. Las dependencias solo apuntan hacia adentro, y las pruebas de arquitectura lo verifican.

```
src/
  TaskBoard.Domain          Agregados, eventos de dominio y reglas de negocio. Sin dependencias.
  TaskBoard.Application     Comandos, consultas, handlers, validadores y abstracciones (CQRS por funcionalidad).
  TaskBoard.Infrastructure  EF Core, repositorios, JWT, hash de contraseñas, almacenamiento de archivos, outbox.
  TaskBoard.Api             Endpoints Minimal API, hub de SignalR, middleware y composition root.
tests/
  TaskBoard.Domain.UnitTests
  TaskBoard.Application.UnitTests
  TaskBoard.Infrastructure.IntegrationTests   PostgreSQL real con Testcontainers.
  TaskBoard.Api.FunctionalTests               La API completa en memoria con WebApplicationFactory.
  TaskBoard.ArchitectureTests                 Reglas de dependencia entre capas.
```

Decisiones de diseño principales:

- **Handlers y decoradores**: cada comando y consulta pasa por decoradores de logging, validación y autorización por tablero.
- **Patrón Result**: los handlers devuelven `Result`/`Result<T>` en lugar de lanzar excepciones para errores esperados; los endpoints los convierten en Problem Details.
- **Outbox transaccional**: los eventos de dominio se guardan como mensajes de outbox en la misma transacción y un proceso en segundo plano los despacha, enviando luego notificaciones y mensajes de SignalR. Cada instancia reserva un lote con `FOR UPDATE SKIP LOCKED` y un plazo (`LockedUntil`), así que con varias instancias cada mensaje se procesa una sola vez; si una instancia muere, otra retoma su lote cuando vence el plazo.
- **Autorización por tablero**: las peticiones que tocan un tablero se validan contra el rol del usuario en ese tablero antes de ejecutar el handler. Los tableros archivados rechazan cualquier escritura. Los roles se guardan en caché 30 segundos y se invalidan en cuanto cambian los miembros del tablero o del equipo.
- **Posiciones fraccionarias**: mover una tarjeta o columna solo cambia su propia posición. Cuando el hueco entre dos vecinas se vuelve demasiado pequeño, la columna (o el tablero) se renumera conservando el orden.

## Cómo empezar

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (ver `global.json`)
- [Docker](https://www.docker.com/) para PostgreSQL, Azurite, Redis y las pruebas de integración

### Todo con Docker

```bash
docker compose up --build
```

Levanta PostgreSQL, Azurite y Redis, aplica las migraciones con el servicio `migrations` y arranca la API en `http://localhost:8080` (entorno `Production`, sin Scalar). Comprueba que todo está bien en `http://localhost:8080/health`.

### API con `dotnet run`

1. Levantar solo las dependencias:

   ```bash
   docker compose up -d postgres azurite redis
   ```

2. Ejecutar la API:

   ```bash
   dotnet run --project src/TaskBoard.Api
   ```

En el entorno `Development` la API aplica las migraciones pendientes al arrancar, y el perfil de `launchSettings.json` define una clave JWT de desarrollo. La API escucha en `http://localhost:5080` y la referencia interactiva está en `http://localhost:5080/scalar`.

## Configuración

La configuración está en `src/TaskBoard.Api/appsettings.json`, con los valores de desarrollo en `appsettings.Development.json`. Fuera de desarrollo, sobrescríbelos con variables de entorno (por ejemplo `ConnectionStrings__Database`) o [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets).

Los secretos nunca van en los appsettings. La clave JWT llega por la variable de entorno `Jwt__SigningKey`; si falta, la API no arranca y lo indica con un mensaje claro.

| Clave | Descripción |
| --- | --- |
| `ConnectionStrings:Database` | Cadena de conexión de PostgreSQL. |
| `ConnectionStrings:Redis` | Cadena de conexión de Redis. Vacía con una sola instancia; obligatoria con varias (backplane de SignalR y caché de roles). |
| `Jwt:Issuer`, `Jwt:Audience` | Emisor y audiencia del token. |
| `Jwt:SigningKey` | Clave simétrica de firma, mínimo 32 caracteres. Solo por variable de entorno (`Jwt__SigningKey`) o user secrets. |
| `Jwt:AccessTokenLifetime` | Duración del token de acceso (`hh:mm:ss`, 15 minutos por defecto). |
| `Jwt:RefreshTokenLifetime` | Duración del refresh token (7 días por defecto). |
| `Jwt:RefreshTokenCleanupInterval` | Cada cuánto se borran los refresh tokens vencidos (1 día por defecto; `00:00:00` lo desactiva). Los revocados se conservan hasta vencer, para detectar su reutilización. |
| `FileStorage:Provider` | `Local` o `AzureBlob`. |
| `FileStorage:LocalRootPath` | Carpeta de los archivos subidos cuando se usa `Local`. |
| `FileStorage:AzureConnectionString` | Cadena de conexión de Blob Storage (`UseDevelopmentStorage=true` para Azurite). |
| `FileStorage:AzureContainerName` | Contenedor de blobs para los adjuntos. |
| `FileStorage:OrphanCleanupInterval`, `FileStorage:OrphanGracePeriod` | Cada cuánto se borran los archivos sin adjunto, y la antigüedad mínima para borrarlos. Un intervalo de `00:00:00` desactiva la limpieza. |
| `Outbox:PollingInterval`, `Outbox:BatchSize` | Ajustes del procesador del outbox. |
| `Outbox:LockDuration` | Cuánto tiempo queda reservado un lote para una instancia. Debe superar lo que tarda en procesarse. |
| `RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:Window` | Intentos por IP permitidos en `/auth/login` y `/auth/register` en cada ventana (10 por minuto por defecto). |
| `ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks` | IPs (`10.0.0.5`) y redes (`10.0.0.0/8`) de los proxies inversos de confianza. Solo de ellos se acepta `X-Forwarded-For`; vacío, no se confía en ninguno. |
| `Cors:AllowedOrigins` | Orígenes del frontend que pueden llamar a la API y al hub. |

## API

Todos los endpoints excepto `/auth/*` y `/health*` requieren un token bearer. Los listados paginados aceptan `page` (desde 1) y `pageSize` (50 por defecto, máximo 200) y responden con `items`, `page`, `pageSize`, `totalCount` y `hasNextPage`.

| Método | Ruta | Descripción |
| --- | --- | --- |
| `POST` | `/auth/register` | Registrar un usuario. Limitado por IP. |
| `POST` | `/auth/login` | Obtener un token de acceso y un refresh token. Limitado por IP. |
| `POST` | `/auth/refresh` | Cambiar un refresh token por tokens nuevos. El anterior deja de servir; reutilizarlo cierra todas las sesiones del usuario. |
| `POST` | `/auth/logout` | Revocar un refresh token. |
| `GET` | `/me` | Datos del usuario actual. |
| `POST` | `/me/password` | Cambiar la contraseña. Revoca todos los refresh tokens del usuario. |
| `GET` | `/teams` | Listar mis equipos. |
| `POST` | `/teams` | Crear un equipo. |
| `POST` | `/teams/{teamId}/members` | Agregar un miembro al equipo. |
| `PATCH` | `/teams/{teamId}/members/{userId}` | Cambiar el rol de un miembro del equipo. |
| `DELETE` | `/teams/{teamId}/members/{userId}` | Quitar a un miembro del equipo, o salir del equipo con tu propio id. También lo quita de los tableros del equipo. |
| `GET` | `/teams/{teamId}/boards` | Listar los tableros de un equipo; `?archived=true` lista los archivados. |
| `POST` | `/teams/{teamId}/boards` | Crear un tablero. |
| `GET` | `/boards/{boardId}` | Obtener un tablero con sus columnas y tarjetas. |
| `PATCH` | `/boards/{boardId}` | Renombrar un tablero. |
| `POST` | `/boards/{boardId}/archive` | Archivar un tablero. Queda de solo lectura. |
| `POST` | `/boards/{boardId}/restore` | Restaurar un tablero archivado. |
| `POST` | `/boards/{boardId}/members` | Agregar un miembro al tablero. |
| `DELETE` | `/boards/{boardId}/members/{userId}` | Quitar a un miembro del tablero. |
| `POST` | `/boards/{boardId}/columns` | Crear una columna. |
| `PATCH` | `/columns/{columnId}` | Renombrar una columna. |
| `PATCH` | `/columns/{columnId}/move` | Mover una columna entre dos columnas vecinas. |
| `POST` | `/columns/{columnId}/cards` | Crear una tarjeta. |
| `GET` | `/boards/{boardId}/cards` | Buscar y filtrar tarjetas: `text`, `columnId`, `assigneeId`, `priority`, `dueBefore`, `archived`. Paginado. |
| `GET` | `/cards/{cardId}` | Obtener una tarjeta con sus miembros asignados, adjuntos y número de comentarios. |
| `PUT` | `/cards/{cardId}` | Editar título, descripción, prioridad y fecha límite de una tarjeta. |
| `PATCH` | `/cards/{cardId}/move` | Mover una tarjeta a una columna y posición. |
| `POST` | `/cards/{cardId}/archive` | Archivar una tarjeta. |
| `POST` | `/cards/{cardId}/restore` | Restaurar una tarjeta archivada en su columna. |
| `POST` | `/cards/{cardId}/assignees` | Asignar un miembro a una tarjeta. |
| `DELETE` | `/cards/{cardId}/assignees/{userId}` | Desasignar a un miembro de una tarjeta. |
| `GET` | `/cards/{cardId}/comments` | Listar los comentarios de una tarjeta, del más antiguo al más reciente. Paginado. |
| `POST` | `/cards/{cardId}/comments` | Agregar un comentario. |
| `PATCH` | `/comments/{commentId}` | Editar tu propio comentario. |
| `DELETE` | `/comments/{commentId}` | Borrar un comentario (autor o admin del tablero). |
| `POST` | `/cards/{cardId}/attachments` | Subir un adjunto (multipart, máximo 10 MB). |
| `GET` | `/attachments/{attachmentId}` | Descargar un adjunto. |
| `DELETE` | `/attachments/{attachmentId}` | Borrar un adjunto y su archivo (quien lo subió o admin del tablero). |
| `GET` | `/notifications` | Listar mis notificaciones. |
| `PATCH` | `/notifications/{notificationId}/read` | Marcar una notificación como leída. |
| `GET` | `/health` | Estado de PostgreSQL, el almacenamiento de archivos y Redis (503 si alguno falla). |
| `GET` | `/health/live` | Responde mientras el proceso está vivo, sin revisar dependencias. |

## Tiempo real

Conéctate al hub de SignalR en `/hubs/board`. Los navegadores envían el token en el parámetro `access_token` de la query string.

- Llama a `JoinBoard(boardId)` al abrir un tablero y a `LeaveBoard(boardId)` al salir. Solo los miembros del tablero pueden unirse.
- Eventos de tablero: `BoardRenamed`, `BoardArchived`, `BoardRestored`, `BoardMemberRemoved`, `ColumnCreated`, `ColumnRenamed`, `ColumnMoved`, `CardCreated`, `CardUpdated`, `CardMoved`, `CardArchived`, `CardRestored`, `CardAssigned`, `CardUnassigned`, `CommentAdded`, `CommentEdited`, `CommentDeleted`, `AttachmentAdded`, `AttachmentDeleted`.
- Cuando un usuario pierde acceso a un tablero, el servidor envía `BoardMemberRemoved` con su propio id a cada una de sus conexiones y luego las saca del grupo, para que el cliente pueda cerrar el tablero. Ese mensaje puede llegarle dos veces (el aviso directo y el del grupo), así que el cliente debe tratarlo de forma idempotente.
- Eventos personales: `NotificationReceived`, que llega a todas las conexiones del usuario sin unirse a ningún tablero.
- Con `ConnectionStrings:Redis` configurado, los mensajes llegan a los clientes conectados a cualquier instancia, y cada instancia saca del grupo a sus propias conexiones cuando un usuario pierde acceso.

## Despliegue

- **Imagen**: `src/TaskBoard.Api/Dockerfile` (contexto: la raíz del repositorio) tiene dos targets: `api`, la API (puerto 8080, usuario sin privilegios), y `migrations`, un [bundle de migraciones de EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles).
- **Migraciones**: fuera de `Development` la API no migra al arrancar. Ejecuta la imagen `migrations` una vez por despliegue, antes de la API, con `ConnectionStrings__Database` (o `--connection`). En `docker-compose.yml` es el servicio `migrations`.
- **Secretos**: pasa `Jwt__SigningKey`, `ConnectionStrings__Database` y, si aplica, `FileStorage__AzureConnectionString` y `ConnectionStrings__Redis` como variables de entorno desde el gestor de secretos de la plataforma.
- **Varias instancias**: configura `ConnectionStrings:Redis` y usa Azure Blob Storage (el disco local no se comparte entre instancias). El outbox ya reparte el trabajo entre instancias.
- **Health checks**: usa `/health/live` como liveness y `/health` como readiness.
- **Proxy inverso**: el límite de intentos se aplica por IP. Detrás de un proxy, declara sus direcciones en `ForwardedHeaders:KnownProxies` o `ForwardedHeaders:KnownNetworks`; si no, todos los usuarios comparten el límite del proxy.

## Pruebas

```bash
dotnet test TaskBoard.slnx
```

Las pruebas de integración y funcionales levantan sus propios contenedores de PostgreSQL y aplican las migraciones, así que Docker debe estar corriendo. El CI ejecuta toda la suite y construye las imágenes Docker en cada push y pull request a `main`.

## Migraciones

Las herramientas de EF Core están fijadas como herramienta local:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Nombre> --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api --output-dir Persistence/Migrations
```
