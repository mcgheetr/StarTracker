# StarTracker Helm Chart

Current usage scope:
- Local Docker Desktop Kubernetes validation/demo
- Not used by the primary GitHub cloud deploy workflow (which deploys Lambda + S3 UI)

This chart deploys:

- `api` (`src/StarTracker.Api`) behind a ClusterIP service
- `ui` (Angular web app) behind a ClusterIP service
- One Ingress with:
  - `/api` -> API service
  - `/` -> UI service

## Secrets via AWS Secrets Manager (recommended)

1. Install External Secrets Operator:

```bash
helm repo add external-secrets https://charts.external-secrets.io
helm repo update
helm upgrade --install external-secrets external-secrets/external-secrets \
  --namespace external-secrets --create-namespace
```

2. Create an AWS secret (example JSON value):

```bash
aws secretsmanager create-secret \
  --name /startracker/dev/api \
  --secret-string '{"apiKey":"<your-api-key>"}'
```

3. Apply the `ClusterSecretStore` (uses ESO service account + IRSA):

```bash
kubectl apply -f ./helm/startracker/examples/cluster-secret-store-aws.yaml
```

4. Enable External Secrets in the chart:

```bash
helm upgrade --install startracker ./helm/startracker \
  --namespace startracker --create-namespace \
  --set ingress.host=startracker.example.com \
  --set externalSecrets.enabled=true \
  --set externalSecrets.secretStoreRef.name=aws-secretsmanager \
  --set externalSecrets.remoteRef.key=/startracker/dev/api \
  --set externalSecrets.remoteRef.property=apiKey
```

## Build and Push Images

Example tags:

```bash
docker build -t <account>.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest -f Dockerfile .
docker push <account>.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest

docker build -t <account>.dkr.ecr.us-east-1.amazonaws.com/startracker-ui:latest -f web/Dockerfile web
docker push <account>.dkr.ecr.us-east-1.amazonaws.com/startracker-ui:latest
```

## Install

Manual secret option (without External Secrets):

```bash
kubectl -n startracker create secret generic startracker-api \
  --from-literal=api-key=<your-api-key>
```

Then install:

```bash
helm upgrade --install startracker ./helm/startracker \
  --namespace startracker --create-namespace \
  --set ingress.host=startracker.example.com \
  --set api.secret.existingSecretName=startracker-api
```

## Update Image Tags

```bash
helm upgrade --install startracker ./helm/startracker \
  --namespace startracker \
  --set api.image.tag=<git-sha> \
  --set ui.image.tag=<git-sha>
```

## YAML Editor Notes

Files under `templates/` are Helm templates (Go templating), not plain YAML.
If your editor shows YAML parse errors there, install Helm support and/or configure YAML custom tags for Helm directives.
