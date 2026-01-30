# StarTracker

StarTracker is a small portfolio backend service that helps users locate stars from a given location and optionally record observations.

## Design

1. User calls GET `/api/v1/stars/{target}/position?lat={lat}&lon={lon}&at={utc}` with decimal coordinates (lat/lon in decimal degrees). Inputs are normalized/rounded to 5 decimal places for storage.
2. API returns celestial (`rightAscensionDegrees`, `declinationDegrees`) and horizontal (`azimuthDegrees`, `altitudeDegrees`) coordinates plus a human-friendly `guidance` field.
3. User can POST an observation to `/api/v1/stars/{target}/observations` with RA/Dec as returned and metadata.

## Security & privacy

- Coordinates are treated as sensitive; repository implementations must encrypt coordinate fields before persisting. The development encryption service uses ASP.NET Core Data Protection. A KMS-based envelope encryption is scaffolded for production use.

## Examples

Health:

- `curl -H "X-API-Key: <key>" http://localhost:5000/api/v1/health`

Get position (compass guidance):

- `curl -H "X-API-Key: <key>" "http://localhost:5000/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z"`

Get position (telescope users)

- If you have a telescope that accepts RA/Dec you can use the `rightAscensionDegrees` and `declinationDegrees` fields from the response directly to point your instrument.

Create observation:

- `curl -X POST -H "Content-Type: application/json" -H "X-API-Key: <key>" -d '{"observedAt":"2026-01-29T16:00:00Z","rightAscensionDegrees":80,"declinationDegrees":38.78,"observer":"me","notes":"note"}' http://localhost:5000/api/v1/stars/Vega/observations`

Notes on encryption provider (dev vs prod):

- By default the application uses ASP.NET Core Data Protection for field-level encryption (development/test friendly). To enable the mocked AWS Encryption SDK path configure `appsettings.json` or environment vars:

```json
{"Encryption": { "UseAwsSdk": "true", "KmsKeyId": "alias/mykey" }}
```

- When the AWS SDK integration is wired, the envelope encryptor will delegate to the `AWS.Cryptography.EncryptionSDK` for production-grade envelope handling.



## Terraform scaffold

See `infra/terraform` for a scaffold that creates a KMS key and a DynamoDB table with server-side encryption (SSE) using the KMS key.

## Conventions

- Coordinates: decimal degrees only for now.
- Code style: use global usings and primary constructors where sensible; prefer concise expressions and constants for repeated values such as the API key header.
