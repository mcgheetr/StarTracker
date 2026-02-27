---
name: principal-api-engineer
description: Implement and refactor backend/API code with principal-level engineering standards in C#/.NET, emphasizing DRY design, clean code, clear boundaries, pragmatic abstractions, and maintainable tests. Use when editing endpoints, services, DTOs, validation, or cross-layer API behavior.
---

# Principal API Engineer

Follow this workflow when implementing or refactoring API/backend changes.

## Working style

- Think in domain boundaries first (API, Core, Infrastructure).
- Prefer small, composable units with single responsibilities.
- Keep public contracts stable unless change is explicitly required.
- Optimize for readability and operability over cleverness.

## DRY guardrails

- Eliminate repeated validation and mapping logic.
- Centralize shared rules behind focused abstractions (extension methods, validators, mappers, or services) when duplication appears in 2+ places.
- Avoid premature abstraction: duplicate once, abstract on repeated patterns with clear naming.

## Clean code rules

- Use intention-revealing names for methods, variables, and DTO fields.
- Keep methods short and focused; prefer early returns for invalid states.
- Keep error responses consistent and actionable.
- Keep dependencies explicit through interfaces and constructor injection.
- Keep side effects isolated and easy to test.

## API implementation checklist

1. Confirm request/response contract and backward compatibility.
2. Validate inputs consistently (format + domain constraints).
3. Keep endpoint handlers thin; delegate business logic to Core services.
4. Keep mapping logic centralized (no ad-hoc field transforms spread across handlers).
5. Add/adjust tests for both happy path and validation/edge cases.
6. Update docs when behavior or contracts change.

## Testing standards

- Add targeted unit tests for business logic changes.
- Add endpoint/integration tests for contract and validation behavior.
- Add idempotency tests for applicable API operations (same request repeated yields safe, consistent outcomes without duplicate side effects).
- Assert meaningful outputs, not just status codes.
- Keep tests deterministic (fixed time/data where possible).

## Anti-patterns to avoid

- Large endpoint methods containing business logic and persistence concerns.
- Repeated string literals for validation messages across handlers.
- Hidden behavior in static helpers with implicit global state.
- Ambiguous method names (for example, `Handle`, `Process`, `DoWork`) when specific names are possible.
