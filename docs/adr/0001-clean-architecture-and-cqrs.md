# 1. Clean Architecture with CQRS and no mediator library

Status: accepted

## Context

The app has several modules (teams, boards, cards, comments, attachments, notifications) and cross-cutting
concerns (validation, logging, per-board authorization) that must apply to every use case.

## Decision

- Four layers: Domain, Application, Infrastructure, Api. Dependencies point inward only.
- Application is organized by feature (`Features/<Module>/<UseCase>/`): command or query, handler, validator, response.
- Commands go through repositories and aggregates. Queries read through `IReadDbContext` (untracked) and project
  straight into DTOs. Application references only the EF Core base package for async LINQ, no provider.
- Our own `ICommandHandler` / `IQueryHandler` interfaces, with cross-cutting concerns as decorators registered with
  Scrutor. MediatR became commercial in 2025, and the decorators keep the pipeline explicit.
- Business failures are returned as `Result`/`Error`, not thrown. Exceptions are for unexpected faults.

## Consequences

- Endpoints inject the exact handler they need, which keeps them thin and testable.
- Adding a use case means adding one folder. Nothing else needs registering.
- `TaskBoard.ArchitectureTests` enforces the layer rules.
