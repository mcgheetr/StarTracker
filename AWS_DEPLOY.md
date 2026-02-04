# AWS Deployment Quick Start

## Deployment via GitHub Actions

This repo uses GitHub Actions + Terraform for deployment. See:

- `.github/workflows/deploy.yml`
- `.github/workflows/destroy.yml`

## Manual Deployment

### 1. Prerequisites

```powershell
# Verify tools
aws --version
terraform --version
docker --version

# Configure AWS CLI
aws configure
```

### 2. Choose an API Key

Pick a secure API key (store it safely). You'll pass it to Terraform.

### 3. Build and Push Image

```powershell
# Get account ID
$AccountId = aws sts get-caller-identity --query Account --output text

# Login to ECR
aws ecr get-login-password --region us-east-1 | `
  docker login --username AWS --password-stdin ${AccountId}.dkr.ecr.us-east-1.amazonaws.com

# Build
docker build -t startracker-api:latest -f Dockerfile.lambda .

# Tag and push
docker tag startracker-api:latest ${AccountId}.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest
docker push ${AccountId}.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest
```

### 4. Deploy with Terraform

```powershell
cd infra/terraform
terraform init
terraform plan -var="api_key=your-secure-key"
terraform apply -var="api_key=your-secure-key"
```

### 5. Test

```powershell
$ApiUrl = terraform output -raw api_gateway_url
curl -H "X-API-Key: your-secure-key" "${ApiUrl}/api/v1/health"
```

## Architecture Overview

- **API Gateway** → **Lambda** (container image)
- **Lambda** → **DynamoDB**
- **CloudWatch** for logs and monitoring

## Cost Estimate

~$0-2/month for portfolio usage (Lambda + DynamoDB on-demand)

## Cleanup

```powershell
cd infra/terraform
terraform destroy
```
