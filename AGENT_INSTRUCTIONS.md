# Agent Instructions

See `docs/ai-guardrails.md` for the canonical AI usage and safety guidelines.

You are allowed to:
- Create and modify code in src/ and tests/
- Create Terraform in infra/
- Create GitHub Actions in .github/workflows/
- Update README.md

You are NOT allowed to:
- Add paid services or expensive defaults (avoid ALB unless explicitly requested).
- Add new external dependencies without asking.
- Commit secrets or real credentials anywhere.
- Implement features unrelated to StarTracker.

Operating rules:
- Before writing code, summarize the plan and assumptions.
- Prefer incremental PR-sized changes.
- Every change must include:
  - buildable code
  - updated docs if behavior changes
  - at least minimal tests for core logic

Cost rules:
- Default to scheduled destroy and smallest service size.
- Use tags: owner=todd, purpose=portfolio, env=dev, ttl=24h.
