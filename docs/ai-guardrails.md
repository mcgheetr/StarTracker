# AI Guardrails

This project uses explicit guardrails to keep AI-assisted changes safe, reviewable, and aligned with cost/security goals.

## Allowed Actions
- Modify code in `src/` and `tests/`
- Update documentation (`README.md`, `DESIGN.md`, `AWS_DEPLOY.md`)
- Create Terraform in `infra/`
- Create GitHub Actions in `.github/workflows/`

## Prohibited Actions
- Add paid services or expensive defaults without approval
- Add new external dependencies without asking
- Commit secrets or real credentials
- Implement features unrelated to StarTracker

## Workflow Expectations
- Summarize plan and assumptions before coding
- Prefer PR-sized, incremental changes
- Every change includes:
  - Buildable code
  - Updated docs if behavior changes
  - Minimal tests for core logic

## Cost & Safety
- Use smallest service sizes and scheduled destroy by default
- Tag all infra: `owner=todd`, `purpose=portfolio`, `env=dev`, `ttl=24h`

## Rationale
These rules keep the repository safe for public use and make AI-generated changes easy to review.
