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

Output:
1) Endpoint table (method, route, request, response, status codes)
2) DTO definitions
3) Validation rules
4) Examples (curl)
