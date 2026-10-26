# 2. Board-scoped authorization in the application pipeline

Status: accepted

## Context

Almost every resource (column, card, comment, attachment) belongs to a board, and only board members may access it,
with different rights per role (Viewer, Editor, Admin). Repeating that check in every handler would be easy to forget.

## Decision

- Requests that touch a board resource implement `IBoardScopedRequest` (`Scope`, `ScopeId`, `MinimumRole`).
- `BoardAuthorizationDecorator` resolves the board from the scope and checks the user's role through
  `IBoardAccessService` before the handler runs.
- Non-members get 404 rather than 403, so board ids cannot be probed. Members with too low a role get 403.
- Boards with `Team` visibility give every team member the Viewer role.
- The SignalR hub (`JoinBoard`) and attachment downloads use the same service.

## Consequences

- One place to audit and test the rule.
- Handlers that act on more than one resource (e.g. moving a card to another column) must still check that the
  second resource belongs to the same board.
