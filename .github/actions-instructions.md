# GitHub Actions Instructions

Goal: CI/CD that is simple, secure, and cost-aware for a portfolio AWS deployment.

## Workflows
1) CI: build + test on PRs and main
2) Deploy: manual (workflow_dispatch) and/or on main merges
3) Destroy: scheduled nightly + manual

## Rules
- No secrets in repo. Use GitHub Secrets for AWS creds.
- Use OIDC (preferred) or access keys if OIDC is not configured yet.
- Fail fast: if build/test fails, do not deploy.
- Keep logs readable; do not echo secrets.

## CI Requirements
- `dotnet restore`
- `dotnet build -c Release`
- `dotnet test -c Release`

## Deploy Requirements (AWS)
- Terraform apply using remote backend (S3 + Dynamo lock).
- Build Docker image, push to ECR, update ECS service.
- After deploy, run a simple health check (GET /health).

## Destroy Requirements
- `terraform destroy -auto-approve` against the same state.
- Only destroy resources tagged `purpose=portfolio`.
- Provide a safety check to avoid destroying anything else.

## Cost Guardrails
- Prefer scheduled destroy to prevent 24/7 compute costs.
- Document manual destroy procedure in README.
