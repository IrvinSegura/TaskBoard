# TaskBoard

[![ci](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml)

API de tablero de tareas estilo Kanban construida con ASP.NET Core. Los equipos tienen tableros, los tableros tienen columnas y tarjetas, y cada cambio se envía en tiempo real a los clientes conectados mediante SignalR.

## Funcionalidades

- **Autenticación**: registro e inicio de sesión con tokens de acceso JWT.
- **Equipos**: crear equipos, agregar y quitar miembros, cambiar roles y salir de un equipo.
- **Tableros**: tableros por equipo con miembros, roles y visibilidad; renombrarlos, archivarlos y restaurarlos. Los tableros archivados son de solo lectura.
- **Columnas y tarjetas**: crear, renombrar y reordenar columnas; crear, editar, mover, archivar y restaurar tarjetas, asignar o desasignar miembros, y buscar o filtrar las tarjetas de un tablero.
- **Comentarios y adjuntos**: agregar, editar y borrar comentarios; subir, descargar y borrar archivos (disco local o Azure Blob Storage).
- **Notificaciones**: notificaciones personales con marca de leído.
- **Tiempo real**: eventos de tablero y notificaciones enviados por SignalR.

## Stack tecnológico

| Área | Tecnología |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Persistencia | EF Core 10, PostgreSQL 17 |
| Tiempo real | SignalR |
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
- **Outbox transaccional**: los eventos de dominio se guardan como mensajes de outbox en la misma transacción y un proceso en segundo plano los despacha, enviando luego notificaciones y mensajes de SignalR.
- **Autorización por tablero**: las peticiones que tocan un tablero se validan contra el rol del usuario en ese tablero antes de ejecutar el handler. Los tableros archivados rechazan cualquier escritura.

## Cómo empezar

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (ver `global.json`)
- [Docker](https://www.docker.com/) para PostgreSQL, Azurite y las pruebas de integración

### Ejecutar en local

1. Levantar PostgreSQL y Azurite:

   ```bash
   docker compose up -d
   ```

2. Ejecutar la API:

   ```bash
   dotnet run --project src/TaskBoard.Api
   ```

En el entorno `Development` la API aplica las migraciones pendientes al arrancar. La API escucha en `http://localhost:5080` y la referencia interactiva está en `http://localhost:5080/scalar`.

## Configuración

La configuración está en `src/TaskBoard.Api/appsettings.json`, con los valores de desarrollo en `appsettings.Development.json`. Fuera de desarrollo, sobrescríbelos con [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) o variables de entorno (por ejemplo `Jwt__SigningKey`).

| Clave | Descripción |
| --- | --- |
| `ConnectionStrings:Database` | Cadena de conexión de PostgreSQL. |
| `Jwt:Issuer`, `Jwt:Audience` | Emisor y audiencia del token. |
| `Jwt:SigningKey` | Clave simétrica de firma. Usa un valor largo y aleatorio en cualquier entorno compartido. |
| `Jwt:AccessTokenLifetime` | Duración del token de acceso (`hh:mm:ss`). |
| `FileStorage:Provider` | `Local` o `AzureBlob`. |
| `FileStorage:LocalRootPath` | Carpeta de los archivos subidos cuando se usa `Local`. |
| `FileStorage:AzureConnectionString` | Cadena de conexión de Blob Storage (`UseDevelopmentStorage=true` para Azurite). |
| `FileStorage:AzureContainerName` | Contenedor de blobs para los adjuntos. |
| `Outbox:PollingInterval`, `Outbox:BatchSize` | Ajustes del procesador del outbox. |
| `Cors:AllowedOrigins` | Orígenes del frontend que pueden llamar a la API y al hub. |

## API

Todos los endpoints excepto `/auth/*` requieren un token bearer.

| Método | Ruta | Descripción |
| --- | --- | --- |
| `POST` | `/auth/register` | Registrar un usuario. |
| `POST` | `/auth/login` | Obtener un token de acceso. |
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
| `GET` | `/boards/{boardId}/cards` | Buscar y filtrar tarjetas: `text`, `columnId`, `assigneeId`, `priority`, `dueBefore`, `archived`. |
| `GET` | `/cards/{cardId}` | Obtener una tarjeta con sus miembros asignados, adjuntos y número de comentarios. |
| `PUT` | `/cards/{cardId}` | Editar título, descripción, prioridad y fecha límite de una tarjeta. |
| `PATCH` | `/cards/{cardId}/move` | Mover una tarjeta a una columna y posición. |
| `POST` | `/cards/{cardId}/archive` | Archivar una tarjeta. |
| `POST` | `/cards/{cardId}/restore` | Restaurar una tarjeta archivada en su columna. |
| `POST` | `/cards/{cardId}/assignees` | Asignar un miembro a una tarjeta. |
| `DELETE` | `/cards/{cardId}/assignees/{userId}` | Desasignar a un miembro de una tarjeta. |
| `GET` | `/cards/{cardId}/comments` | Listar los comentarios de una tarjeta. |
| `POST` | `/cards/{cardId}/comments` | Agregar un comentario. |
| `PATCH` | `/comments/{commentId}` | Editar tu propio comentario. |
| `DELETE` | `/comments/{commentId}` | Borrar un comentario (autor o admin del tablero). |
| `POST` | `/cards/{cardId}/attachments` | Subir un adjunto (multipart, máximo 10 MB). |
| `GET` | `/attachments/{attachmentId}` | Descargar un adjunto. |
| `DELETE` | `/attachments/{attachmentId}` | Borrar un adjunto y su archivo (quien lo subió o admin del tablero). |
| `GET` | `/notifications` | Listar mis notificaciones. |
| `PATCH` | `/notifications/{notificationId}/read` | Marcar una notificación como leída. |

## Tiempo real

Conéctate al hub de SignalR en `/hubs/board`. Los navegadores envían el token en el parámetro `access_token` de la query string.

- Llama a `JoinBoard(boardId)` al abrir un tablero y a `LeaveBoard(boardId)` al salir. Solo los miembros del tablero pueden unirse.
- Eventos de tablero: `BoardRenamed`, `BoardArchived`, `BoardRestored`, `BoardMemberRemoved`, `ColumnCreated`, `ColumnRenamed`, `ColumnMoved`, `CardCreated`, `CardUpdated`, `CardMoved`, `CardArchived`, `CardRestored`, `CardAssigned`, `CardUnassigned`, `CommentAdded`, `CommentEdited`, `CommentDeleted`, `AttachmentAdded`, `AttachmentDeleted`.
- Cuando un usuario pierde acceso a un tablero, el servidor saca sus conexiones del grupo. Antes recibe `BoardMemberRemoved` con su propio id, para que el cliente pueda cerrar el tablero.
- Eventos personales: `NotificationReceived`, que llega a todas las conexiones del usuario sin unirse a ningún tablero.

## Pruebas

```bash
dotnet test TaskBoard.slnx
```

Las pruebas de integración y funcionales levantan sus propios contenedores de PostgreSQL y aplican las migraciones, así que Docker debe estar corriendo. El CI ejecuta toda la suite en cada push y pull request a `main`.

## Migraciones

Las herramientas de EF Core están fijadas como herramienta local:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Nombre> --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api --output-dir Persistence/Migrations
```
