# TaskBoard

[![ci](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/IrvinSegura/TaskBoard/actions/workflows/ci.yml)

A Kanban-style task board API built with ASP.NET Core. Teams own boards, boards have columns and cards, and every change is pushed to connected clients in real time through SignalR.

## Features

- **Auth**: registration and login with JWT access tokens.
- **Teams**: create teams, add and remove members, change roles and leave a team.
- **Boards**: team boards with members, roles and visibility; rename, archive and restore them. Archived boards are read-only.
- **Columns and cards**: create, rename and reorder columns; create, edit, move, archive and restore cards, assign or unassign members, and search or filter a board's cards.
- **Comments and attachments**: add, edit and delete comments; upload, download and delete files (local disk or Azure Blob Storage).
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
| `PATCH` | `/teams/{teamId}/members/{userId}` | Change a team member's role. |
| `DELETE` | `/teams/{teamId}/members/{userId}` | Remove a team member, or leave the team with your own id. Also removes them from the team's boards. |
| `GET` | `/teams/{teamId}/boards` | List a team's boards; `?archived=true` lists archived ones. |
| `POST` | `/teams/{teamId}/boards` | Create a board. |
| `GET` | `/boards/{boardId}` | Get a board with its columns and cards. |
| `PATCH` | `/boards/{boardId}` | Rename a board. |
| `POST` | `/boards/{boardId}/archive` | Archive a board. It becomes read-only. |
| `POST` | `/boards/{boardId}/restore` | Restore an archived board. |
| `POST` | `/boards/{boardId}/members` | Add a board member. |
| `DELETE` | `/boards/{boardId}/members/{userId}` | Remove a board member. |
| `POST` | `/boards/{boardId}/columns` | Create a column. |
| `PATCH` | `/columns/{columnId}` | Rename a column. |
| `PATCH` | `/columns/{columnId}/move` | Move a column between two neighbouring columns. |
| `POST` | `/columns/{columnId}/cards` | Create a card. |
| `GET` | `/boards/{boardId}/cards` | Search and filter cards: `text`, `columnId`, `assigneeId`, `priority`, `dueBefore`, `archived`. |
| `GET` | `/cards/{cardId}` | Get a card with its assignees, attachments and comment count. |
| `PUT` | `/cards/{cardId}` | Edit a card's title, description, priority and due date. |
| `PATCH` | `/cards/{cardId}/move` | Move a card to a column and position. |
| `POST` | `/cards/{cardId}/archive` | Archive a card. |
| `POST` | `/cards/{cardId}/restore` | Restore an archived card to its column. |
| `POST` | `/cards/{cardId}/assignees` | Assign a member to a card. |
| `DELETE` | `/cards/{cardId}/assignees/{userId}` | Unassign a member from a card. |
| `GET` | `/cards/{cardId}/comments` | List a card's comments. |
| `POST` | `/cards/{cardId}/comments` | Add a comment. |
| `PATCH` | `/comments/{commentId}` | Edit your own comment. |
| `DELETE` | `/comments/{commentId}` | Delete a comment (author or board admin). |
| `POST` | `/cards/{cardId}/attachments` | Upload an attachment (multipart, max 10 MB). |
| `GET` | `/attachments/{attachmentId}` | Download an attachment. |
| `DELETE` | `/attachments/{attachmentId}` | Delete an attachment and its file (uploader or board admin). |
| `GET` | `/notifications` | List my notifications. |
| `PATCH` | `/notifications/{notificationId}/read` | Mark a notification as read. |

## Real time

Connect to the SignalR hub at `/hubs/board`. Browsers send the token in the `access_token` query string.

- Call `JoinBoard(boardId)` when opening a board and `LeaveBoard(boardId)` when leaving it. Only board members can join.
- Board events: `BoardRenamed`, `BoardArchived`, `BoardRestored`, `BoardMemberRemoved`, `ColumnCreated`, `ColumnRenamed`, `ColumnMoved`, `CardCreated`, `CardUpdated`, `CardMoved`, `CardArchived`, `CardRestored`, `CardAssigned`, `CardUnassigned`, `CommentAdded`, `CommentEdited`, `CommentDeleted`, `AttachmentAdded`, `AttachmentDeleted`.
- When a user loses access to a board, the server removes their connections from its group. They still receive `BoardMemberRemoved` with their own user id first, so the client can close the board.
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
