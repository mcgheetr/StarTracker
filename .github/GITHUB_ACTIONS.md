# GitHub Actions Setup

This repo includes automated CI/CD workflows for testing and deploying StarTracker to AWS Lambda.

## Workflows

### `ci.yml` - Continuous Integration
- **Trigger**: Every push to `main` and pull requests
- **Actions**: Build, test, and verify code quality
- **No secrets required**

### `deploy.yml` - Automated Deployment
- **Trigger**: Manual (`workflow_dispatch`)
- **Actions**: 
  - Run CI tests
  - Build Docker image (Lambda-optimized)
  - Push to ECR
  - Apply Terraform
  - Health check API
- **Requires**: AWS credentials, API key
- **Output**: Deployment summary with live API URL

### `destroy.yml` - Infrastructure Cleanup
- **Trigger**: Daily at 10 PM UTC or manual
- **Actions**: Terraform destroy with safety check
- **Safety**: Only destroys if infrastructure has `purpose=portfolio` tag
- **Requires**: AWS credentials, API key

## Setup Instructions

### 1. Create AWS OIDC Role for GitHub

This allows GitHub Actions to authenticate to AWS without storing access keys.

```bash
# Run this in your AWS account (requires IAM permissions)
# Creates a trust relationship between GitHub and AWS

aws iam create-openid-connect-provider \
  --url "https://token.actions.githubusercontent.com" \
  --client-id-list "sts.amazonaws.com" \
  --thumbprint-list "6938fd4d98bab03faadb97b34396831e3780aea1"

# Then create an IAM role with the trust policy
# See: https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect
```

Or use AWS CloudFormation:
```bash
aws cloudformation create-stack \
  --stack-name github-oidc \
  --template-body file://github-oidc.yaml
```

### 2. Set GitHub Secrets

In your repository settings, add these secrets:

| Secret | Value | Example |
|--------|-------|---------|
| `AWS_OIDC_ROLE_ARN` | Full ARN of OIDC role created above | `arn:aws:iam::645446651173:role/github-actions-role` |
| `API_KEY` | The API key for authentication | `<your-generated-api-key>` |

### 3. Grant Role Permissions

Attach these policies to the OIDC role:
- `AmazonDynamoDBFullAccess`
- `AWSLambda_FullAccess`
- `AmazonAPIGatewayAdministrator`
- `AmazonEC2ContainerRegistryFullAccess`

### 4. Run Your First Deploy

```bash
# Push to main to trigger CI tests
git push origin main

# Navigate to Actions tab and manually trigger deploy workflow
# Or use GitHub CLI:
gh workflow run deploy.yml -f environment=dev -f region=us-east-1
```

## Example: Manual Deployment via CLI

```bash
gh workflow run deploy.yml \
  --ref main \
  -f environment=dev \
  -f region=us-east-1
```

## Troubleshooting

**Deploy fails with "No valid credential sources"**
- Verify `AWS_OIDC_ROLE_ARN` secret is set correctly
- Ensure OIDC provider is created in AWS account
- Check role trust policy includes GitHub Actions

**API health check fails**
- Wait 30-60 seconds for Lambda to become ready
- Check CloudWatch logs: `aws logs tail /aws/lambda/startracker-dev --follow`

**Destroy workflow fails**
- Verify Terraform state has `purpose=portfolio` output
- Check state file in `infra/terraform/terraform.tfstate`

## Cost Considerations

- **Lambda**: ~$0-2/month (free tier eligible)
- **DynamoDB**: ~$0-5/month (on-demand, free tier eligible)
- **ECR**: ~$0.50/month (minimal storage)
- **GitHub Actions**: Free for public repos, 2,000 minutes/month for private

**Total estimated cost**: ~$0-10/month (free tier friendly)

## Next Steps

1. Set up OIDC role in AWS account
2. Add secrets to GitHub repository
3. Test CI workflow by pushing to main
4. Manually trigger deploy workflow
5. Monitor logs and adjust as needed
