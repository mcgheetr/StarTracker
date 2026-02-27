# MCP Astronomy Integration Spec (StarTracker)

## Goal

Enable StarTracker to support many stars and constellations (beyond Polaris) by introducing a dedicated MCP-backed astronomy data service that returns authoritative sky object coordinates and visibility data.

## Current State

- API route already accepts variable target: `GET /api/v1/stars/{target}/position`.
- Backend currently returns fixed Polaris coordinates regardless of target.
- Frontend can already submit arbitrary target names and render sky maps.

## Desired Outcome

- Keep existing API contract stable for current consumers.
- Resolve user-selected targets dynamically from an astronomy catalog.
- Return accurate RA/Dec and Az/Alt for observer location + time.
- Add catalog/search capability for selectable stars/constellations.

---

## Architecture Overview

### Components

1. **StarTracker API**
   - Remains public contract surface for the web app.
   - Delegates object resolution/coordinates to Core abstractions.

2. **Core Astronomy Abstraction**
   - New interface for catalog lookup + coordinate retrieval.
   - Owns domain-level request/response models.

3. **Infrastructure MCP Client**
   - Calls MCP server tools/resources.
   - Handles retries, timeout, translation of MCP errors to domain errors.

4. **Astronomy MCP Server** (new external/internal service)
   - Exposes object resolution and coordinate computation tools.
   - Uses authoritative astronomy catalogs/libraries under the hood.

### Sequence: `GET /stars/{target}/position`

1. API validates `target`, `lat`, `lon`, `at`.
2. API invokes `IAstronomyCatalogService.GetObjectPositionAsync(...)`.
3. MCP client resolves target and fetches coordinates.
4. API composes response (existing DTO shape) + guidance text.
5. API returns 200 or problem details error.

---

## Proposed Core Contracts

### New Interface

`src/StarTracker.Core/Interfaces/IAstronomyCatalogService.cs`

Methods:

- `Task<CelestialObjectResult> ResolveObjectAsync(string target, CancellationToken ct)`
- `Task<EquatorialCoordinatesResult> GetEquatorialAsync(string objectId, DateTimeOffset at, CancellationToken ct)`
- `Task<HorizontalCoordinatesResult> GetHorizontalAsync(string objectId, ObserverLocation observer, DateTimeOffset at, CancellationToken ct)`
- `Task<IReadOnlyList<CelestialObjectSummary>> SearchObjectsAsync(string query, int limit, CancellationToken ct)`
- `Task<IReadOnlyList<CelestialObjectSummary>> ListObjectsAsync(ObjectListFilter filter, CancellationToken ct)`

### New Core Models

- `CelestialObjectResult`
  - `ObjectId`, `DisplayName`, `Aliases[]`, `ObjectType`, `Constellation?`, `Magnitude?`
- `EquatorialCoordinatesResult`
  - `RightAscensionDegrees`, `DeclinationDegrees`, `Epoch`, `ComputedAt`
- `HorizontalCoordinatesResult`
  - `AzimuthDegrees`, `AltitudeDegrees`, `ComputedAt`
- `ObserverLocation`
  - `LatitudeDegrees`, `LongitudeDegrees`, `ElevationMeters?`
- `ObjectListFilter`
  - `ObjectType[]`, `MaxMagnitude?`, `Constellation?`, `Limit`

---

## MCP Tool Contract (Spec)

### Tool: `resolve_object`

Input:

```json
{ "target": "Polaris" }
```

Output:

```json
{
  "objectId": "hip:11767",
  "displayName": "Polaris",
  "aliases": ["Alpha Ursae Minoris"],
  "objectType": "star",
  "constellation": "UMi",
  "magnitude": 1.97
}
```

### Tool: `get_equatorial_coordinates`

Input:

```json
{ "objectId": "hip:11767", "at": "2026-01-29T16:00:00Z" }
```

Output:

```json
{
  "rightAscensionDegrees": 37.954,
  "declinationDegrees": 89.264,
  "epoch": "J2000",
  "computedAt": "2026-01-29T16:00:00Z"
}
```

### Tool: `get_horizontal_coordinates`

Input:

```json
{
  "objectId": "hip:11767",
  "at": "2026-01-29T16:00:00Z",
  "observer": { "lat": 37.70443, "lon": -77.41832, "elevationMeters": 100 }
}
```

Output:

```json
{
  "azimuthDegrees": 359.5,
  "altitudeDegrees": 37.7,
  "computedAt": "2026-01-29T16:00:00Z"
}
```

### Tool: `search_objects`

Input:

```json
{ "query": "pol", "limit": 10 }
```

Output:

```json
{
  "items": [
    { "objectId": "hip:11767", "displayName": "Polaris", "objectType": "star", "magnitude": 1.97 }
  ]
}
```

### Tool: `list_objects`

Input:

```json
{ "objectTypes": ["star", "constellation"], "maxMagnitude": 3.0, "limit": 200 }
```

Output:

```json
{
  "items": [
    { "objectId": "hip:11767", "displayName": "Polaris", "objectType": "star", "magnitude": 1.97 }
  ]
}
```

---

## API Changes (StarTracker)

### Keep Existing Endpoint

`GET /api/v1/stars/{target}/position`

Behavior change (internal only):

- Replace fixed Polaris lookup with `IAstronomyCatalogService`.
- Preserve response shape:
  - `Target`
  - `RightAscensionDegrees`
  - `DeclinationDegrees`
  - `AzimuthDegrees`
  - `AltitudeDegrees`
  - `Guidance`
  - `At`

### Add New Optional Endpoints

1. `GET /api/v1/catalog/search?q={query}&limit={n}`
2. `GET /api/v1/catalog/objects?type=star&type=constellation&maxMagnitude=3&limit=200`

These enable frontend dropdowns/autocomplete without changing position endpoint usage.

---

## Error Handling & Idempotency

### Error Mapping

- `target not found` -> 404 problem details (`detail: target not found`)
- `invalid observer coordinates` -> 400
- MCP timeout/downstream failure -> 503 with retriable message
- unexpected mapping error -> 500

### Idempotency Expectations

- `GET` operations remain naturally idempotent.
- Repeated requests with same `(target, lat, lon, at)` should return equivalent results (within documented float tolerance).
- If retry logic is added in MCP client, avoid duplicate side effects (tools should be read-only for these operations).

---

## Caching & Performance

- Cache resolved object metadata (`resolve_object`) by normalized target for short TTL (e.g., 1–24h).
- Optionally cache position responses by `(objectId, lat, lon, atRoundedMinute)` for short TTL.
- Use timeout budget and cancellation propagation:
  - API request timeout > MCP client timeout > provider timeout.

---

## Security & Operational Requirements

- Do not send API secrets to MCP unless required.
- Sanitize/log minimal target + request correlation ID.
- Emit structured logs around:
  - MCP call duration
  - failure category
  - target resolution hit/miss
- Add health indicator for MCP dependency readiness (optional degraded mode).

---

## Rollout Plan

### Phase 1: Contracts + Adapter Skeleton

- Add new Core interfaces/models.
- Add Infrastructure MCP adapter with feature flag off by default.

### Phase 2: Endpoint Integration

- Wire position endpoint to abstraction.
- Keep fallback to legacy mapper if feature flag disabled.

### Phase 3: Catalog Endpoints + UI Selector

- Add catalog/search endpoints.
- UI consumes catalog for dropdown/autocomplete.

### Phase 4: Hardening

- Add caching, observability, and failure policy tuning.
- Remove legacy Polaris-only path once validated.

---

## Test Plan

### Unit Tests

- Target normalization and resolution mapping.
- Error mapping from MCP failures to problem details categories.
- Guidance generation still works with dynamic targets.

### Endpoint Tests

- Valid target returns non-empty coordinates.
- Unknown target returns 404.
- Invalid `lat/lon` still returns 400.
- Response contract remains backward compatible.

### Idempotency Tests

- Repeat same GET request N times with same `(target, lat, lon, at)` and assert equivalent results (tolerance-based float compare).
- Assert no duplicate persistence side effects in position flow.

### Integration/Contract Tests

- MCP tool payload schema compatibility tests.
- Timeouts/retries behavior under simulated MCP failures (including fail-fast after 5 repeated errors).

---

## Decisions Locked for Initial Implementation

1. Compute Az/Alt in MCP.
2. Use the provided HIP bright-star list as initial source-of-truth catalog seed.
3. Use idempotency equivalence tolerance defaults:
   - RightAscensionDegrees/DeclinationDegrees: ±0.0005°
   - AzimuthDegrees/AltitudeDegrees: ±0.001°
4. Accept aliases in `target` and canonicalize to a primary object identity.
5. Apply fail-fast fault policy after 5 repeated MCP errors for the same operation window.
