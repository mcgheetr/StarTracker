Define the REST API for StarTracker.

Requirements:
- Versioned routes: /api/v1
- Endpoints:
  - GET /stars/{target}/position?lat=&lon=&at=
  - POST /stars/{target}/observations
  - GET /stars/{target}/observations?from=&to=
  - GET /observations/{id}
  - GET /health
- Use API key auth via X-API-Key header.
- Use ProblemDetails for errors.
- Use DTOs in Core as records.
- Use DateTimeOffset in UTC.

Security & Coordinate handling:
- All API traffic MUST use HTTPS (TLS).
- Requests must include `X-API-Key`. If `ApiKey` is configured, it must match.
- Coordinates are considered sensitive data. Do **not** log raw lat/lon or store them unencrypted.
- Accept coordinates as decimal degrees (e.g., `lat=37.70443&lon=-77.41832`). Inputs may have fewer than 5 decimal places; server normalizes/rounds to exactly **5 decimal places** before storage (padding with zeros if needed). Reject invalid ranges.
- `GET /stars/{target}/position` returns:
  - `target`, `rightAscensionDegrees`, `declinationDegrees`, `azimuthDegrees`, `altitudeDegrees`, `at` (UTC), `guidance` (human text).
- Use `ProblemDetails` for parse/validation errors.

Output:
1) Endpoint table (method, route, request, response, status codes)
2) DTO definitions
3) Validation rules
4) Examples (curl)
