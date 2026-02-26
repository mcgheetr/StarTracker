# StarTracker  

[![CI](https://github.com/mcgheetr/StarTracker/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/mcgheetr/StarTracker/actions/workflows/ci.yml)
[![Deploy](https://github.com/mcgheetr/StarTracker/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/mcgheetr/StarTracker/actions/workflows/deploy.yml)
[![Destroy Infrastructure](https://github.com/mcgheetr/StarTracker/actions/workflows/destroy.yml/badge.svg)](https://github.com/mcgheetr/StarTracker/actions/workflows/destroy.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Terraform](https://img.shields.io/badge/Terraform-1.5+-623CE4?logo=terraform)](https://www.terraform.io/)
[![AWS](https://img.shields.io/badge/AWS-Lambda-FF9900?logo=amazonaws)](https://aws.amazon.com/lambda/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

StarTracker is a portfolio‑ready backend that turns a user’s location into clear, friendly star‑finding guidance — and stores observations with privacy‑first encryption.

## **Highlights**

- Clean layered architecture (API → Core → Infrastructure)
- Field‑level encryption for sensitive coordinates
- DynamoDB storage with a queryable GSI
- CI/CD with Terraform + GitHub Actions

## **Quickstart**

**Prereqs**

- .NET 10 SDK
- Docker (optional, for DynamoDB Local)

**Run locally (in‑memory repo)**

```bash
dotnet run --project src/StarTracker.Api
```

**Health check**

```bash
curl -H "X-API-Key: <key>" http://localhost:5000/api/v1/health
```

## **Swagger**

- UI: `http://localhost:5115/swagger`
- JSON: `http://localhost:5115/swagger/v1/swagger.json`
- Click **Authorize** and enter the `X-API-Key` value

## **API Examples**

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

## **Architecture**

See `DESIGN.md` for the full system design and rationale.

- Context diagram source: `docs/architecture/context.mmd`
- Rendered diagram: `docs/architecture/context.svg`

![StarTracker context diagram](docs/architecture/context.svg)

## **Security & Privacy**

- Coordinates are encrypted before storage.
- Dev uses ASP.NET Core Data Protection.
- KMS envelope encryption is scaffolded for production.

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

**DynamoDB Local**

```bash
docker-compose up -d dynamodb-local
.\scripts\init-dynamodb-local.ps1
dotnet run --project src/StarTracker.Api --launch-profile DynamoDBLocal
```

**Full stack with Docker Compose (API + UI + DynamoDB Local)**

```bash
docker compose up --build
```

Then open `http://localhost:4200`.

Stop and remove containers:

```bash
docker compose down
```

## **Local API Key**

The API always requires `X-API-Key` to be present.

**Docker Compose**

- API key is fixed in `docker-compose.yml`:
  - `ApiKey: local-dev-key`
- Use `local-dev-key` in the UI/API requests.

**Non-Docker local (`dotnet run`)**

- By default, no specific key is configured in `appsettings*.json`.
- Any non-empty `X-API-Key` value is accepted unless you set `ApiKey`.
- To require a specific key locally, set an environment variable before running:

```bash
$env:ApiKey="my-local-key"
dotnet run --project src/StarTracker.Api
```

## **Terraform + CI/CD**

### **Deployment Scope**

- GitHub Actions deploy (`.github/workflows/deploy.yml`) is the cloud path:
  - API -> AWS Lambda container image
  - UI -> S3 static website hosting (no CloudFront required)
- Helm/Kubernetes (`helm/startracker`) is currently for local Docker Desktop Kubernetes validation and demo use.

Bootstrap remote state (one‑time):

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

Main stack:

```bash
cd infra/terraform
terraform init
terraform plan -var="api_key=your-secure-key"
terraform apply -var="api_key=your-secure-key"
```

## **Local Kubernetes (Docker Desktop Only)**

Use this when you want to validate the Helm chart locally without provisioning AWS EKS.

```bash
kubectl config use-context docker-desktop
kubectl create namespace startracker
kubectl -n startracker create secret generic startracker-api --from-literal=api-key=local-dev-key
docker build -t startracker-api:local -f Dockerfile .
docker build -t startracker-ui:local -f web/Dockerfile web
helm upgrade --install startracker ./helm/startracker \
  --namespace startracker \
  --set ingress.enabled=false \
  --set externalSecrets.enabled=false \
  --set api.image.repository=startracker-api \
  --set api.image.tag=local \
  --set ui.image.repository=startracker-ui \
  --set ui.image.tag=local \
  --set api.secret.existingSecretName=startracker-api
kubectl -n startracker port-forward svc/startracker-startracker-ui 4200:80
```

Open:

- UI: `http://localhost:4200`
- Swagger through UI proxy: `http://localhost:4200/api/swagger/index.html`

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

## **License**

MIT licensed. You’re free to use, modify, and distribute this project with attribution. See `LICENSE` for details.

## **AI Guardrails**

AI usage rules and safety constraints are documented in `docs/ai-guardrails.md`.

## Built By
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Todd%20McGhee-0A66C2?style=flat&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/tmcg33)
