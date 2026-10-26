# 3. Realtime updates through an outbox and SignalR

Status: accepted

## Context

Clients viewing a board must see changes made by others, and users must receive personal notifications.
Sending a SignalR message inside the request could announce a change whose transaction later fails.

## Decision

- Aggregates raise domain events. An EF Core interceptor stores them as `OutboxMessage` rows in the same
  `SaveChanges`, so an event exists only if its change was committed.
- `OutboxProcessor` (a background service) polls pending messages and dispatches them to `IDomainEventHandler<T>`.
- Handlers push through `IRealtimeNotifier`, implemented with a typed hub (`Hub<IBoardClient>`).
- Groups: `board:{id}` for everyone viewing a board. Personal notifications use SignalR's user addressing (JWT `sub`).

## Consequences

- Delivery is at-least-once and delayed by the polling interval (500 ms by default).
- Running several API instances needs a SignalR backplane (Redis or Azure SignalR) and row locking in the outbox.
