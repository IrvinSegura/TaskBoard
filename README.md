# TaskBoard

[![ci](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml)

Tablero de tareas estilo Kanban: una API construida con ASP.NET Core y un frontend en React. Los equipos tienen tableros, los tableros tienen columnas y tarjetas, y cada cambio se envía en tiempo real a los clientes conectados mediante SignalR.

## Funcionalidades

- **Autenticación**: registro e inicio de sesión con tokens de acceso JWT de corta duración y refresh tokens rotativos; cierre de sesión, cambio de contraseña, `GET /me` y cambio del nombre visible. Límite de intentos en el login y el registro.
- **Equipos**: crear, renombrar y eliminar equipos (solo el dueño elimina), agregar y quitar miembros, cambiar roles y salir de un equipo.
- **Tableros**: tableros por equipo con miembros, roles y visibilidad; renombrarlos, archivarlos, restaurarlos y eliminarlos (solo si están archivados). Los tableros archivados son de solo lectura.
- **Columnas y tarjetas**: crear, renombrar, reordenar y eliminar columnas (con sus tarjetas); crear, editar, mover, archivar, restaurar y eliminar tarjetas archivadas, asignar o desasignar miembros, y buscar o filtrar las tarjetas de un tablero.
- **Etiquetas y checklists**: etiquetas de colores por tablero que se ponen en las tarjetas y sirven de filtro; una checklist por tarjeta, con su avance en el tablero.
- **Comentarios y adjuntos**: agregar, editar y borrar comentarios; subir, descargar y borrar archivos (disco local o Azure Blob Storage).
- **Notificaciones**: avisos cuando te asignan una tarjeta, te mencionan con `@`, comentan en una tarjeta tuya o te agregan a un equipo o tablero; con marca de leído, marcar todas como leídas y eliminarlas.
- **Actividad**: historial de quién creó, movió, editó, comentó o archivó qué, por tarjeta, tablero, equipo y en general.
- **Favoritos y calendario**: marcar equipos y tableros con estrella para tenerlos a mano, y un calendario mensual con las fechas límite y la actividad de todos tus tableros.
- **Chat**: mensajes directos entre usuarios, grupos y un canal por equipo y por tablero, con enlaces, imágenes, audio (incluidas notas de voz grabadas en el navegador), video y documentos; editar y borrar mensajes, y contador de no leídos.
- **Tiempo real**: eventos de tablero, notificaciones y chat enviados por SignalR.
- **Frontend**: aplicación web en React con tableros arrastrables, detalle de tarjetas, comentarios, adjuntos, filtros, notificaciones en vivo y modo claro/oscuro.
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
| Frontend | React 19, TypeScript, Vite, Tailwind CSS 4, TanStack Query, React Router, dnd-kit, Radix UI |
| Pruebas del frontend | Vitest, Testing Library |

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
web/
  src/app          Router, proveedores y páginas de error.
  src/components   Sistema de diseño (botones, diálogos, menús...) y layout (sidebar, barra superior).
  src/features     Un módulo por funcionalidad: auth, account, teams, boards, cards, notifications, dashboard.
  src/lib          Cliente HTTP con refresh de sesión, conexión de SignalR, tema y utilidades.
```

Decisiones de diseño principales:

- **Handlers y decoradores**: cada comando y consulta pasa por decoradores de logging, validación y autorización por tablero.
- **Patrón Result**: los handlers devuelven `Result`/`Result<T>` en lugar de lanzar excepciones para errores esperados; los endpoints los convierten en Problem Details.
- **Menciones**: en el cuerpo de un comentario se guardan como `@[Nombre](user:id)`. Al crearse el comentario se avisa a cada persona mencionada que tenga acceso al tablero y al resto de los asignados de la tarjeta.
- **Historial de actividad**: cada mensaje del outbox guarda quién hizo el cambio (`ActorId`), y un proyector convierte los eventos en líneas de actividad con los nombres de ese momento, para que el historial se lea igual aunque luego se borre la tarjeta.
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

### Frontend

Requiere [Node.js 24](https://nodejs.org/). Con la API corriendo en `http://localhost:5080`:

```bash
cd web
npm install
npm run dev
```

La aplicación queda en `http://localhost:5173`, el único origen que la API acepta por CORS en desarrollo (`Cors:AllowedOrigins`). Para apuntar a otra API, copia `web/.env.example` a `web/.env` y cambia `VITE_API_URL`.

| Script | Descripción |
| --- | --- |
| `npm run dev` | Servidor de desarrollo con recarga en caliente. |
| `npm run build` | Comprueba los tipos y genera la versión de producción en `web/dist`. |
| `npm run lint` | ESLint. |
| `npm run typecheck` | Solo la comprobación de tipos. |
| `npm test` | Pruebas con Vitest. |

Cómo se conecta con la API:

- **Sesión**: el token de acceso vive solo en memoria y el refresh token en la cookie `HttpOnly`. Al cargar la página se pide uno nuevo con `POST /auth/refresh`; si una petición recibe un 401, el cliente refresca una vez y la reintenta. Las pestañas se turnan para refrescar con la Web Locks API, así que nunca se envía dos veces la misma cookie, y cerrar sesión en una pestaña la cierra en las demás.
- **Datos**: TanStack Query guarda en caché las respuestas. Mover tarjetas y columnas se refleja al instante (actualización optimista) y se deshace si la API lo rechaza.
- **Tiempo real**: una sola conexión de SignalR por pestaña. Al abrir un tablero el cliente llama a `JoinBoard`, y cada evento vuelve a pedir los datos afectados. Si el usuario pierde acceso al tablero, se le saca de él.
- **Permisos**: la interfaz oculta o desactiva lo que el rol del usuario en el tablero no permite, y los tableros archivados se muestran de solo lectura; la API sigue siendo quien valida.

### Datos de demostración

Con la API corriendo, llena la base local con 8 personas, 3 equipos, 7 tableros y unas 40 tarjetas con etiquetas, checklists, comentarios y adjuntos:

```bash
cd web
npm run seed:demo
```

Entra con `laura@demo.taskboard.test`; la contraseña de todas las cuentas de demostración está en `scripts/seed-demo.mjs`. Para otra API, define `API_URL`. El script se niega a correr si los datos ya existen.

Para ver los datos con tu propia cuenta (ya registrada), compártelos con ella: te agrega como admin a los equipos y tableros de demostración y te asigna las mismas tarjetas que a Laura. Se puede repetir sin problema.

```bash
npm run seed:demo -- --share-with tu@email.com
```

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

Todos los endpoints excepto `/auth/*` y `/health*` requieren un token bearer.

El refresh token nunca viaja en el JSON: va en la cookie `taskboard-refresh-token` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/auth`), que el JavaScript de la página no puede leer. El frontend guarda el token de acceso solo en memoria, llama a `/auth/*` con `credentials: 'include'` y, al cargar la página, pide uno nuevo con `POST /auth/refresh`. Por `SameSite=Strict`, el frontend y la API tienen que estar en el mismo sitio: el mismo dominio o subdominios de uno (en local, `localhost` con cualquier puerto). Si hay varias pestañas abiertas, solo una debe refrescar a la vez; dos refresh con la misma cookie cuentan como reutilización y cierran todas las sesiones. Los listados paginados aceptan `page` (desde 1) y `pageSize` (50 por defecto, máximo 200) y responden con `items`, `page`, `pageSize`, `totalCount` y `hasNextPage`.

| Método | Ruta | Descripción |
| --- | --- | --- |
| `POST` | `/auth/register` | Registrar un usuario. Limitado por IP. |
| `POST` | `/auth/login` | Devuelve el token de acceso en el body y guarda el refresh token en una cookie. Limitado por IP. |
| `POST` | `/auth/refresh` | Cambiar el refresh token de la cookie por tokens nuevos. El anterior deja de servir; reutilizarlo cierra todas las sesiones del usuario. |
| `POST` | `/auth/logout` | Revocar el refresh token de la cookie y borrarla. |
| `GET` | `/me` | Datos del usuario actual. |
| `PATCH` | `/me` | Cambiar el nombre visible (`displayName`). |
| `GET` | `/me/cards` | Mis tarjetas asignadas, activas y de tableros no archivados, por fecha límite. |
| `POST` | `/me/password` | Cambiar la contraseña. Revoca todos los refresh tokens del usuario y borra la cookie, así que hay que volver a iniciar sesión. |
| `GET` | `/teams` | Listar mis equipos. |
| `POST` | `/teams` | Crear un equipo. |
| `PATCH` | `/teams/{teamId}` | Renombrar un equipo. Solo dueños y admins. |
| `DELETE` | `/teams/{teamId}` | Eliminar un equipo con todos sus tableros. Solo el dueño. |
| `GET` | `/teams/{teamId}/members` | Listar los miembros del equipo con su nombre, email y rol. El dueño aparece primero. |
| `POST` | `/teams/{teamId}/members` | Agregar un miembro al equipo por su email (`email`, `role`). Solo dueños y admins. |
| `PATCH` | `/teams/{teamId}/members/{userId}` | Cambiar el rol de un miembro del equipo. |
| `DELETE` | `/teams/{teamId}/members/{userId}` | Quitar a un miembro del equipo, o salir del equipo con tu propio id. También lo quita de los tableros del equipo. |
| `GET` | `/teams/{teamId}/boards` | Listar los tableros de un equipo; `?archived=true` lista los archivados. |
| `POST` | `/teams/{teamId}/boards` | Crear un tablero. |
| `GET` | `/boards/{boardId}` | Obtener un tablero con sus columnas y tarjetas. |
| `PATCH` | `/boards/{boardId}` | Renombrar un tablero. |
| `POST` | `/boards/{boardId}/archive` | Archivar un tablero. Queda de solo lectura. |
| `POST` | `/boards/{boardId}/restore` | Restaurar un tablero archivado. |
| `DELETE` | `/boards/{boardId}` | Eliminar un tablero archivado con todo su contenido. Solo admins del tablero. |
| `POST` | `/boards/{boardId}/members` | Agregar un miembro al tablero. |
| `DELETE` | `/boards/{boardId}/members/{userId}` | Quitar a un miembro del tablero. |
| `POST` | `/boards/{boardId}/columns` | Crear una columna. |
| `PATCH` | `/columns/{columnId}` | Renombrar una columna. |
| `PATCH` | `/columns/{columnId}/move` | Mover una columna entre dos columnas vecinas. |
| `DELETE` | `/columns/{columnId}` | Eliminar una columna y sus tarjetas, también las archivadas. Solo admins del tablero. |
| `POST` | `/columns/{columnId}/cards` | Crear una tarjeta. |
| `GET` | `/boards/{boardId}/cards` | Buscar y filtrar tarjetas: `text`, `columnId`, `assigneeId`, `labelId`, `priority`, `dueBefore`, `archived`. Paginado. |
| `GET` | `/cards/{cardId}` | Obtener una tarjeta con sus miembros asignados, adjuntos y número de comentarios. |
| `PUT` | `/cards/{cardId}` | Editar título, descripción, prioridad y fecha límite de una tarjeta. |
| `PATCH` | `/cards/{cardId}/move` | Mover una tarjeta a una columna y posición. |
| `POST` | `/cards/{cardId}/archive` | Archivar una tarjeta. |
| `POST` | `/cards/{cardId}/restore` | Restaurar una tarjeta archivada en su columna. |
| `DELETE` | `/cards/{cardId}` | Eliminar una tarjeta archivada con sus comentarios y adjuntos. Solo admins del tablero. |
| `POST` | `/cards/{cardId}/assignees` | Asignar un miembro a una tarjeta. |
| `DELETE` | `/cards/{cardId}/assignees/{userId}` | Desasignar a un miembro de una tarjeta. |
| `POST` | `/boards/{boardId}/labels` | Crear una etiqueta (`name`, `color`). Editores y admins. |
| `PUT` | `/boards/{boardId}/labels/{labelId}` | Cambiar el nombre y el color de una etiqueta. |
| `DELETE` | `/boards/{boardId}/labels/{labelId}` | Eliminar una etiqueta; se quita de todas sus tarjetas. |
| `POST` | `/cards/{cardId}/labels` | Poner una etiqueta del tablero en una tarjeta (`labelId`). |
| `DELETE` | `/cards/{cardId}/labels/{labelId}` | Quitar una etiqueta de una tarjeta. |
| `POST` | `/cards/{cardId}/checklist` | Añadir un elemento a la checklist (`text`). |
| `PATCH` | `/cards/{cardId}/checklist/{itemId}` | Cambiar el texto o marcar un elemento (`text`, `isDone`; solo lo que se envía). |
| `DELETE` | `/cards/{cardId}/checklist/{itemId}` | Eliminar un elemento de la checklist. |
| `GET` | `/cards/{cardId}/comments` | Listar los comentarios de una tarjeta, del más antiguo al más reciente. Paginado. |
| `POST` | `/cards/{cardId}/comments` | Agregar un comentario. |
| `PATCH` | `/comments/{commentId}` | Editar tu propio comentario. |
| `DELETE` | `/comments/{commentId}` | Borrar un comentario (autor o admin del tablero). |
| `POST` | `/cards/{cardId}/attachments` | Subir un adjunto (multipart, máximo 10 MB). |
| `GET` | `/attachments/{attachmentId}` | Descargar un adjunto. |
| `DELETE` | `/attachments/{attachmentId}` | Borrar un adjunto y su archivo (quien lo subió o admin del tablero). |
| `GET` | `/me/activity` | Actividad de todos los tableros que puedo ver, de la más reciente a la más antigua. Paginado. |
| `GET` | `/teams/{teamId}/activity` | Actividad de los tableros del equipo que puedo ver. Paginado. |
| `GET` | `/boards/{boardId}/activity` | Actividad de un tablero. Paginado. |
| `GET` | `/cards/{cardId}/activity` | Historial de una tarjeta. Paginado. |
| `GET` | `/me/calendar?from=&to=` | Tarjetas activas con fecha límite y actividad entre `from` (incluido) y `to` (excluido), de todos los tableros que puedo ver. Rango de hasta 45 días; la actividad trae como mucho 1000 entradas, las más recientes (`activityTruncated` avisa si había más). |
| `GET` | `/me/favorites` | Mis equipos y tableros favoritos (sin los archivados ni los que ya no puedo ver). |
| `PUT` | `/me/favorites/boards/{boardId}` | Marcar un tablero como favorito. Idempotente. |
| `DELETE` | `/me/favorites/boards/{boardId}` | Quitar un tablero de favoritos. Idempotente. |
| `PUT` | `/me/favorites/teams/{teamId}` | Marcar un equipo como favorito. Idempotente. |
| `DELETE` | `/me/favorites/teams/{teamId}` | Quitar un equipo de favoritos. Idempotente. |
| `GET` | `/chat/conversations` | Mis conversaciones (directas, grupos y los canales de mis equipos y tableros no archivados), con el último mensaje y los no leídos, de la más reciente a la más antigua. |
| `GET` | `/chat/conversations/{conversationId}` | Una conversación. 404 si no existe o no puedo abrirla. |
| `GET` | `/chat/contacts` | Las personas con las que comparto un equipo o tablero. |
| `POST` | `/chat/conversations/direct` | Abrir (o crear la primera vez) la conversación directa con alguien, por `userId` o `email`. |
| `POST` | `/chat/conversations/group` | Crear un grupo con `title` y `participantIds` (hasta 50 personas). |
| `PATCH` | `/chat/conversations/{conversationId}` | Renombrar un grupo. |
| `POST` | `/chat/conversations/{conversationId}/participants` | Agregar personas a un grupo. |
| `DELETE` | `/chat/conversations/{conversationId}/participants/me` | Salir de un grupo. |
| `POST` | `/chat/conversations/{conversationId}/read` | Marcar la conversación como leída. |
| `GET` | `/chat/conversations/{conversationId}/messages` | Mensajes, los más recientes primero por página y cada página del más antiguo al más reciente; `before` (id de un mensaje) trae los anteriores. |
| `POST` | `/chat/conversations/{conversationId}/messages` | Enviar un mensaje (multipart): campo `body` y archivos `files`. Hasta 10 archivos de 25 MB, 50 MB por mensaje. |
| `PATCH` | `/chat/messages/{messageId}` | Editar el texto de un mensaje propio. |
| `DELETE` | `/chat/messages/{messageId}` | Borrar un mensaje propio: queda como "mensaje eliminado" y sus archivos se borran. |
| `GET` | `/chat/attachments/{attachmentId}` | Descargar un archivo del chat. |
| `GET` | `/notifications` | Listar mis notificaciones. |
| `PATCH` | `/notifications/{notificationId}/read` | Marcar una notificación como leída. |
| `POST` | `/notifications/read-all` | Marcar todas mis notificaciones como leídas. |
| `DELETE` | `/notifications/{notificationId}` | Eliminar una notificación. |
| `GET` | `/health` | Estado de PostgreSQL, el almacenamiento de archivos y Redis (503 si alguno falla). |
| `GET` | `/health/live` | Responde mientras el proceso está vivo, sin revisar dependencias. |

## Tiempo real

Conéctate al hub de SignalR en `/hubs/board`. Los navegadores envían el token en el parámetro `access_token` de la query string.

- Llama a `JoinBoard(boardId)` al abrir un tablero y a `LeaveBoard(boardId)` al salir. Solo los miembros del tablero pueden unirse.
- Eventos de tablero: `BoardRenamed`, `BoardArchived`, `BoardRestored`, `BoardDeleted`, `BoardMemberRemoved`, `ColumnCreated`, `ColumnRenamed`, `ColumnMoved`, `ColumnDeleted`, `CardCreated`, `CardUpdated`, `CardMoved`, `CardArchived`, `CardRestored`, `CardDeleted`, `CardAssigned`, `CardUnassigned`, `LabelsChanged`, `CardLabelsChanged`, `ChecklistChanged`, `CommentAdded`, `CommentEdited`, `CommentDeleted`, `AttachmentAdded`, `AttachmentDeleted`.
- Cuando un usuario pierde acceso a un tablero, el servidor envía `BoardMemberRemoved` con su propio id a cada una de sus conexiones y luego las saca del grupo, para que el cliente pueda cerrar el tablero. Ese mensaje puede llegarle dos veces (el aviso directo y el del grupo), así que el cliente debe tratarlo de forma idempotente.
- Eventos personales: `NotificationReceived` y `TeamDeleted` (a cada miembro de un equipo eliminado), que llegan a todas las conexiones del usuario sin unirse a ningún tablero.
- Eventos de chat, a todas las conexiones de quienes pueden abrir la conversación (sin unirse a nada): `ChatMessageReceived`, `ChatMessageUpdated`, `ChatMessageDeleted`, `ConversationUpdated` (grupo renombrado o con otras personas; también le llega a quien salió) y `ConversationRead` (solo a quien leyó, para sus otras pestañas).
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

Las pruebas de integración y funcionales levantan sus propios contenedores de PostgreSQL y aplican las migraciones, así que Docker debe estar corriendo. Las del frontend se ejecutan con `npm test` dentro de `web`. El CI ejecuta toda la suite, revisa el frontend (lint, tipos, pruebas y build) y construye las imágenes Docker en cada push y pull request a `main`.

## Migraciones

Las herramientas de EF Core están fijadas como herramienta local:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Nombre> --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api --output-dir Persistence/Migrations
```
