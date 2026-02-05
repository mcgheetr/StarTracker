# StarTracker

StarTracker is a portfolio‑ready backend service for locating stars from a user’s location and recording observations with privacy‑first storage.

**Highlights**
- Minimal API design with clean layering (API → Core → Infrastructure)
- Field‑level encryption for sensitive coordinates
- CI/CD‑ready with Terraform + GitHub Actions
- DynamoDB storage with a queryable GSI

**Status**
- ✅ Local dev and tests
- ✅ AWS deploy via GitHub Actions
- ✅ Remote Terraform state (S3 + DynamoDB)

**Quick Links**
- Live API: `https://2ujy1g942f.execute-api.us-east-1.amazonaws.com/`
- Swagger UI: `http://localhost:5115/swagger`
- Terraform: `infra/terraform`

**Core Flow**
1. GET `/api/v1/stars/{target}/position?lat={lat}&lon={lon}&at={utc}`
2. Receive RA/Dec, azimuth/altitude, and guidance text
3. POST observations to `/api/v1/stars/{target}/observations`

## **API Usage**

**Health**
```bash
curl -H "X-API-Key: <key>" http://localhost:5000/api/v1/health
```

**Get position**
```bash
curl -H "X-API-Key: <key>" \
  "http://localhost:5000/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z"
```

**Create observation**
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "X-API-Key: <key>" \
  -d '{"observedAt":"2026-01-29T16:00:00Z","rightAscensionDegrees":80,"declinationDegrees":38.78,"observer":"me","notes":"note"}' \
  http://localhost:5000/api/v1/stars/Vega/observations
```

**Swagger**
- UI: `http://localhost:5115/swagger`
- JSON: `http://localhost:5115/swagger/v1/swagger.json`
- Click **Authorize** and enter the `X-API-Key` value

## **Live Deployment (AWS)**

Base URL (Dev):
```
https://2ujy1g942f.execute-api.us-east-1.amazonaws.com/
```

Endpoints:
- `GET /api/v1/health`
- `GET /api/v1/stars/{target}/position?lat={lat}&lon={lon}&at={utc}`
- `POST /api/v1/stars/{target}/observations`
- `GET /api/v1/stars/{target}/observations`
- `GET /api/v1/observations/{id}`

Example:
```bash
curl -H "X-API-Key: <key>" https://2ujy1g942f.execute-api.us-east-1.amazonaws.com/api/v1/health
```

## **Security & Privacy**

- Coordinates are treated as sensitive and encrypted before storage.
- Development uses ASP.NET Core Data Protection.
- AWS KMS envelope encryption is scaffolded for production.

Config example:
```json
{"Encryption": { "UseAwsSdk": "true", "KmsKeyId": "alias/mykey" }}
```

## **Local Development**

**Build**
```bash
dotnet build -c Release
```

**Test**
```bash
dotnet test -c Release --logger "console;verbosity=normal"
```

**Run API (in‑memory repo)**
```bash
dotnet run --project src/StarTracker.Api
```

**Run with DynamoDB Local**
```bash
docker-compose up -d dynamodb-local
.\scripts\init-dynamodb-local.ps1
dotnet run --project src/StarTracker.Api --launch-profile DynamoDBLocal
```

## **Terraform + CI/CD**

Bootstrap remote state (one‑time):
```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

Main Terraform stack:
```bash
cd infra/terraform
terraform init
terraform plan -var="api_key=your-secure-key"
terraform apply -var="api_key=your-secure-key"
```

## **Configuration**

Repository selection:
```json
{
  "Repository": { "Type": "InMemory" }
}
```

DynamoDB selection:
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

## **Conventions**

- Coordinates are decimal degrees.
- Prefer concise expressions and constants (e.g., `X-API-Key` header).
