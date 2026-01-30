Security & Coordinates (supplement)

Coordinate handling:
- API accepts decimal degrees (lat/lon) in query or body. Inputs may have fewer than 5 decimal places; server normalizes/rounds to exactly 5 decimal places for storage (padding with zeros if needed).
- Validate lat in [-90,90] and lon in [-180,180].
- Do not accept non-decimal formats for now; DMS support may be added later.

Data protection:
- Coordinates are sensitive: do **not** log raw lat/lon or store them unencrypted.
- Core must define `IEncryptionService` with `Protect(string)`/`Unprotect(string)` contracts; implement `DataProtectionEncryptionService` in Infrastructure for development/testing.
- In production, implement `KmsEncryptionService` (envelope encryption) and ensure DynamoDB tables use SSE and appropriate KMS keys.
- Repositories must encrypt coordinate payloads (e.g., serialized lat/lon or RA/Dec) prior to persisting and decrypt on read.

Position response guidance:
- `GET /stars/{target}/position` should return: `target`, `rightAscensionDegrees`, `declinationDegrees`, `azimuthDegrees`, `altitudeDegrees`, `at` (UTC), and `guidance` (human text; e.g., "Look due north at 70 degrees for the brightest object, that's Polaris!").