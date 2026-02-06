Create GitHub Actions workflows for StarTracker.

Requirements:
- CI workflow: runs on PR + main push, builds and tests .NET solution.
- Deploy workflow:
  - manual trigger (workflow_dispatch)
  - runs CI steps
  - builds Docker image and pushes to ECR
  - terraform apply
  - updates ECS service
  - health check on /health
- Destroy workflow:
  - nightly schedule + manual trigger
  - terraform destroy
  - includes a safety check: only destroy when purpose=portfolio tag is present in state outputs.

Constraints:
- Prefer OIDC role assumption; if not possible, include access key method as alternative.
- No secrets printed.
- Keep workflows readable and concise.

Output:
- .github/workflows/ci.yml
- .github/workflows/deploy.yml
- .github/workflows/destroy.yml
- Explanation of required GitHub Secrets/Variables
