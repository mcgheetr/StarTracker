# Copilot Instructions (Repo-Wide)

You are assisting on a portfolio backend service written in C# using the latest LTS .NET (use .NET 8 unless explicitly instructed otherwise).
Primary goal: clean, readable, production-lean backend code that demonstrates solid engineering judgment.

## Non-Negotiables
- Do not add new dependencies unless explicitly requested.
- Keep solutions minimal: prefer built-in .NET libraries over third-party packages.
- Every change must compile. Add/update tests when behavior changes.
- Never log secrets, API keys, tokens, or PII.
- Do not invent APIs or infrastructure that is not described in the repo.

## Project Style / Architecture
- Use a clean separation:
  - `src/StarTracker.Api` (Minimal API host)
  - `src/StarTracker.Core` (domain + interfaces + contracts)
  - `src/StarTracker.Infrastructure` (AWS/DynamoDB/etc implementations)
  - `tests/StarTracker.Tests` (unit + small integration tests)
- Core must have NO cloud SDK dependencies.
- Infrastructure depends on AWS SDK and implements interfaces from Core.
- Api references Core + Infrastructure and wires DI.

## C# Code Style
- C# 12 / .NET 8 conventions.
- Enable nullable reference types. Avoid null returns; prefer Result types or explicit 404s.
- Prefer `record` for DTOs and immutable data. Use classes when necessary.
- Use `async/await` end-to-end for I/O.
- Prefer early returns; avoid deep nesting.
- Prefer explicit types when it improves clarity; `var` is fine when obvious.
- Use `DateTimeOffset` for timestamps (UTC).
- Prefer `CancellationToken` on public async methods.

## API Design Conventions
- Use RESTful patterns with correct status codes:
  - 200/201 for success, 400 validation, 401/403 auth, 404 not found, 409 conflicts, 429 rate limit if used, 500 only for unexpected.
- Use consistent route patterns:
  - `/api/v1/stars/{target}/position`
  - `/api/v1/stars/{target}/observations`
- Validate inputs at boundary (API layer). Keep Core logic strict.
- Return problem details for errors using built-in ASP.NET Core mechanisms.

## Logging & Diagnostics
- Use `ILogger<T>`; log meaningful events, not noise.
- Log at:
  - Information: high-level request outcomes
  - Warning: recoverable issues, validation rejections
  - Error: unexpected failures
- Include correlation id if present (use `TraceIdentifier`).

## Testing Expectations
- Tests should be deterministic and fast.
- Use xUnit. Keep mocks minimal; prefer testing behavior.
- Infrastructure tests should avoid real AWS calls (use local emulation only if requested).
- Aim for: Core logic coverage + API endpoint behavior smoke tests.

## Security
- API Key auth via header (e.g., `X-API-Key`) unless otherwise specified.
- Secrets must come from environment variables or a secret store, never hard-coded.
- Use least privilege assumptions in IaC.

## Documentation
- Update README when adding endpoints, env vars, or infra steps.
- Prefer short, runnable examples (curl) over long prose.

## Output Format
When generating code:
1) Brief plan
2) Files changed/added with paths
3) Code blocks per file
4) Notes on how to run/tests
