# TaskBoard

[![ci](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml)

A Kanban-style task board API built with ASP.NET Core. Teams own boards, boards have columns and cards, and every change is pushed to connected clients in real time through SignalR.

## Features

- **Auth**: registration and login with JWT access tokens.
- **Teams**: create teams and add members with roles.
- **Boards**: team boards with members, roles and visibility.
- **Columns and cards**: create columns, create cards, move cards between columns and assign members.
- **Comments and attachments**: comment on cards and upload files (local disk or Azure Blob Storage).
- **Notifications**: personal notifications with read tracking.
- **Real time**: board events and notifications pushed over SignalR.

## Tech stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Persistence | EF Core 10, PostgreSQL 17 |
| Real time | SignalR |
| Validation | FluentValidation |
| File storage | Local disk or Azure Blob Storage (Azurite locally) |
| Logging | Serilog |
| API docs | OpenAPI + Scalar |
| Tests | xUnit, Shouldly, NSubstitute, Testcontainers, NetArchTest |

## Architecture

The solution follows Clean Architecture. Dependencies point inwards only, and the architecture tests enforce it.

```
src/
  TaskBoard.Domain          Aggregates, domain events, business rules. No dependencies.
  TaskBoard.Application     Commands, queries, handlers, validators and abstractions (CQRS by feature).
  TaskBoard.Infrastructure  EF Core, repositories, JWT, password hashing, file storage, outbox.
  TaskBoard.Api             Minimal API endpoints, SignalR hub, middleware, composition root.
tests/
  TaskBoard.Domain.UnitTests
  TaskBoard.Application.UnitTests
  TaskBoard.Infrastructure.IntegrationTests   Real PostgreSQL through Testcontainers.
  TaskBoard.Api.FunctionalTests               Full API in memory through WebApplicationFactory.
  TaskBoard.ArchitectureTests                 Layer dependency rules.
```

Key design points:

- **Handlers and decorators**: every command and query goes through logging, validation and board authorization decorators.
- **Result pattern**: handlers return `Result`/`Result<T>` instead of throwing for expected errors; endpoints map them to Problem Details.
- **Transactional outbox**: domain events are saved as outbox messages in the same transaction and dispatched by a background processor, which then fans out notifications and SignalR messages.
- **Board-scoped authorization**: requests that touch a board are checked against the user's board role before the handler runs.

## Getting started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (see `global.json`)
- [Docker](https://www.docker.com/) for PostgreSQL, Azurite and the integration tests

### Run locally

1. Start PostgreSQL and Azurite:

   ```bash
   docker compose up -d
   ```

2. Run the API:

   ```bash
   dotnet run --project src/TaskBoard.Api
   ```

In the `Development` environment the API applies pending migrations on startup. The API listens on `http://localhost:5080` and the interactive API reference is at `http://localhost:5080/scalar`.

## Configuration

Settings live in `src/TaskBoard.Api/appsettings.json`, with development values in `appsettings.Development.json`. Override them with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables (for example `Jwt__SigningKey`) outside development.

| Key | Description |
| --- | --- |
| `ConnectionStrings:Database` | PostgreSQL connection string. |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer and audience. |
| `Jwt:SigningKey` | Symmetric signing key. Use a long random value in any shared environment. |
| `Jwt:AccessTokenLifetime` | Access token lifetime (`hh:mm:ss`). |
| `FileStorage:Provider` | `Local` or `AzureBlob`. |
| `FileStorage:LocalRootPath` | Folder for uploaded files when using `Local`. |
| `FileStorage:AzureConnectionString` | Blob Storage connection string (`UseDevelopmentStorage=true` for Azurite). |
| `FileStorage:AzureContainerName` | Blob container for attachments. |
| `Outbox:PollingInterval`, `Outbox:BatchSize` | Outbox processor tuning. |
| `Cors:AllowedOrigins` | Frontend origins allowed to call the API and the hub. |

## API

All endpoints except `/auth/*` require a bearer token.

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/auth/register` | Register a user. |
| `POST` | `/auth/login` | Get an access token. |
| `GET` | `/teams` | List my teams. |
| `POST` | `/teams` | Create a team. |
| `POST` | `/teams/{teamId}/members` | Add a team member. |
| `GET` | `/teams/{teamId}/boards` | List a team's boards. |
| `POST` | `/teams/{teamId}/boards` | Create a board. |
| `GET` | `/boards/{boardId}` | Get a board with its columns and cards. |
| `POST` | `/boards/{boardId}/members` | Add a board member. |
| `POST` | `/boards/{boardId}/columns` | Create a column. |
| `POST` | `/columns/{columnId}/cards` | Create a card. |
| `PATCH` | `/cards/{cardId}/move` | Move a card to a column and position. |
| `POST` | `/cards/{cardId}/assignees` | Assign a member to a card. |
| `GET` | `/cards/{cardId}/comments` | List a card's comments. |
| `POST` | `/cards/{cardId}/comments` | Add a comment. |
| `POST` | `/cards/{cardId}/attachments` | Upload an attachment (multipart, max 10 MB). |
| `GET` | `/attachments/{attachmentId}` | Download an attachment. |
| `GET` | `/notifications` | List my notifications. |
| `PATCH` | `/notifications/{notificationId}/read` | Mark a notification as read. |

## Real time

Connect to the SignalR hub at `/hubs/board`. Browsers send the token in the `access_token` query string.

- Call `JoinBoard(boardId)` when opening a board and `LeaveBoard(boardId)` when leaving it. Only board members can join.
- Board events: `ColumnCreated`, `CardCreated`, `CardMoved`, `CardAssigned`, `CommentAdded`, `AttachmentAdded`.
- Personal events: `NotificationReceived`, delivered to every connection of the user without joining a board.

## Tests

```bash
dotnet test TaskBoard.slnx
```

Integration and functional tests start their own PostgreSQL containers, so Docker must be running. CI runs the full suite on every push and pull request to `main`.

## Migrations

EF Core tools are pinned as a local tool:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Name> --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api --output-dir Persistence/Migrations
```
