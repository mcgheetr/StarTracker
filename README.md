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

Swagger (OpenAPI):

- UI: `http://localhost:5115/swagger`
- JSON: `http://localhost:5115/swagger/v1/swagger.json`
- API key: click **Authorize** and enter the `X-API-Key` value

Get position (compass guidance):

- `curl -H "X-API-Key: <key>" "http://localhost:5000/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z"`

Get position (telescope users)

- If you have a telescope that accepts RA/Dec you can use the `rightAscensionDegrees` and `declinationDegrees` fields from the response directly to point your instrument.

Create observation:

- `curl -X POST -H "Content-Type: application/json" -H "X-API-Key: <key>" -d '{"observedAt":"2026-01-29T16:00:00Z","rightAscensionDegrees":80,"declinationDegrees":38.78,"observer":"me","notes":"note"}' http://localhost:5000/api/v1/stars/Vega/observations`

### Live Deployment (AWS)

The API is deployed to AWS Lambda with API Gateway:

**Base URL (Dev):** `https://2ujy1g942f.execute-api.us-east-1.amazonaws.com/`

**Endpoints:**
- `GET /api/v1/health` - Health check
- `GET /api/v1/stars/{target}/position?lat={lat}&lon={lon}&at={utc}` - Get star position
- `POST /api/v1/stars/{target}/observations` - Create observation
- `GET /api/v1/stars/{target}/observations` - Get observations for a target
- `GET /api/v1/observations/{id}` - Get observation by ID

Example:
```bash
curl -H "X-API-Key: <key>" https://2ujy1g942f.execute-api.us-east-1.amazonaws.com/api/v1/health
```

Notes on encryption provider (dev vs prod):

- By default the application uses ASP.NET Core Data Protection for field-level encryption (development/test friendly). To enable the mocked AWS Encryption SDK path configure `appsettings.json` or environment vars:

```json
{"Encryption": { "UseAwsSdk": "true", "KmsKeyId": "alias/mykey" }}
```

- When the AWS SDK integration is wired, the envelope encryptor will delegate to the `AWS.Cryptography.EncryptionSDK` for production-grade envelope handling.



## Development

### Build and test locally

Build the solution:
```bash
dotnet build -c Release
```

Run tests with visible output (shows all passed/failed tests):
```bash
dotnet test -c Release --logger "console;verbosity=normal"
```

Note: The `--logger "console;verbosity=normal"` flag is required to see test output and results in the terminal. Without it, test results are not displayed.

Run a specific test project:
```bash
dotnet test tests/StarTracker.Tests -c Release --logger "console;verbosity=normal"
```

### Run the API locally

Prerequisites:
- .NET 8 SDK
- Optional: set `ApiKey` in `appsettings.Development.json` or via `ApiKey` environment variable

**With InMemory Repository (default):**
```bash
dotnet run --project src/StarTracker.Api
```

**With DynamoDB Local:**

First, start DynamoDB Local using Docker:
```bash
docker-compose up -d dynamodb-local
```

Initialize the DynamoDB table:
```bash
.\scripts\init-dynamodb-local.ps1
```

Then run the API with DynamoDB Local configuration:
```bash
dotnet run --project src/StarTracker.Api --launch-profile DynamoDBLocal
```

The API will start on `http://localhost:5115`. Use the examples in the [Examples](#examples) section above.

To stop DynamoDB Local:
```bash
docker-compose down
```

### Repository Configuration

The repository can be switched via `appsettings.json`:

```json
{
  "Repository": {
    "Type": "InMemory"
  }
}
```

Or for DynamoDB (see `appsettings.DynamoDBLocal.json` for example):
```json
{
  "Repository": {
    "Type": "DynamoDB",
    "DynamoDB": {
      "ServiceUrl": "http://localhost:8000",
      "Region": "us-east-1",
      "TableName": "observations"
    }
  }
}
```

## Terraform scaffold

See `infra/terraform` for the deployment scaffold. Before running CI/CD, bootstrap remote state:

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

## Conventions

- Coordinates: decimal degrees only for now.
- Code style: use global usings and primary constructors where sensible; prefer concise expressions and constants for repeated values such as the API key header.
