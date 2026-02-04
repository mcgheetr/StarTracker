# StarTracker Infrastructure

Terraform configuration for deploying StarTracker to **AWS Lambda** (free-tier friendly).

## Architecture

```
┌──────────────┐      ┌──────────────┐
│ API Gateway  │────> │   Lambda     │
│  (HTTP API)  │      │  Container   │
└──────────────┘      └──────────────┘
                             │
                             └──▶ DynamoDB
```

## Resources Created

- **Lambda**: Container-based Lambda using the AWS Lambda Web Adapter
- **ECR**: Container registry for Lambda images
- **API Gateway**: HTTP API with Lambda proxy integration
- **DynamoDB**: Table with GSI and on-demand billing
- **IAM**: Least-privilege role for Lambda
- **CloudWatch**: Log groups for API Gateway and Lambda

## Prerequisites

1. **AWS CLI** configured with credentials:
   ```bash
   aws configure
   ```

2. **Terraform** installed (>= 1.0):
   ```bash
   terraform version
   ```

3. **Docker** for building and pushing images

4. **API Key**: Choose a secure API key (stored in Terraform variables).

5. **(Optional) Create S3 + DynamoDB for remote state**:
   ```bash
   # S3 bucket for state
   aws s3api create-bucket \
     --bucket startracker-terraform-state \
     --region us-east-1

   aws s3api put-bucket-versioning \
     --bucket startracker-terraform-state \
     --versioning-configuration Status=Enabled

   aws s3api put-bucket-encryption \
     --bucket startracker-terraform-state \
     --server-side-encryption-configuration '{
       "Rules": [{
         "ApplyServerSideEncryptionByDefault": {
           "SSEAlgorithm": "AES256"
         }
       }]
     }'

   # DynamoDB table for state locking
   aws dynamodb create-table \
     --table-name startracker-terraform-locks \
     --attribute-definitions AttributeName=LockID,AttributeType=S \
     --key-schema AttributeName=LockID,KeyType=HASH \
     --billing-mode PAY_PER_REQUEST \
     --region us-east-1
   ```

   Then uncomment the backend configuration in `main.tf`.

## Deployment Steps

### 1. Build and Push Docker Image

```bash
# Authenticate Docker to ECR (run this from project root)
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Build the Lambda image
docker build -t startracker-api:latest -f Dockerfile.lambda .

# Tag for ECR
docker tag startracker-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest

# Push to ECR
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/startracker-dev:latest
```

### 2. Initialize Terraform

```bash
cd infra/terraform
terraform init
```

### 3. Plan and Apply

```bash
# Review what will be created
terraform plan -var="api_key=your-secure-key"

# Apply changes
terraform apply -var="api_key=your-secure-key"
```

### 4. Get API Gateway URL

```bash
terraform output api_gateway_url
```

### 5. Test the Deployment

```bash
# Get the API Gateway URL from output
API_URL=$(terraform output -raw api_gateway_url)

# Test health endpoint
curl -H "X-API-Key: your-api-key" ${API_URL}/api/v1/health

# Test position endpoint
curl -H "X-API-Key: your-api-key" \
  "${API_URL}/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z"
```

## Configuration Variables

Create a `terraform.tfvars` file to customize:

```hcl
region         = "us-east-1"
environment    = "dev"
app_name       = "startracker"
image_tag      = "v1.0.0"   # Docker image tag
api_key        = "your-secure-key"  # API key for requests
```

## Cost Optimization

- **Lambda Free Tier**: 1M requests and 400k GB-seconds per month (always free)
- **API Gateway Free Tier**: 1M requests per month (12 months)
- **DynamoDB On-Demand**: Scales to zero when unused
- **ECR Lifecycle**: Automatically deletes old images

## Estimated Monthly Costs (us-east-1, minimal usage)

- **Lambda**: $0 (free tier)
- **API Gateway**: $0 (free tier)
- **DynamoDB**: ~$0-1/month
- **ECR**: ~$0 (storage only)

**Total: ~$0-2/month** for portfolio usage

## Monitoring

- **CloudWatch Logs**: `/aws/lambda/startracker-dev` and `/aws/apigateway/startracker-dev`
- **Lambda metrics**: Invocations, duration, errors
- **API Gateway metrics**: Request count, latency, errors

## Cleanup

```bash
# Destroy all resources
terraform destroy

# Manually delete ECR images first if needed
aws ecr batch-delete-image \
  --repository-name startracker-dev \
  --image-ids imageTag=latest
```

## Security Notes

- ✅ Lambda execution role with least privilege
- ✅ DynamoDB encryption at rest (AWS-managed)
- ✅ API key authentication via environment variable
- ✅ CloudWatch logs for audit trail

## Troubleshooting

**Lambda invocation fails:**
- Check CloudWatch logs: `/aws/lambda/startracker-dev`
- Verify the Lambda image exists in ECR
- Confirm the `api_key` variable was set during apply

**API Gateway returns 503:**
- Check Lambda function health in AWS Console
- Verify API Gateway integration targets the Lambda ARN

**Lambda can't access DynamoDB:**
- Check Lambda execution role IAM permissions
- Confirm the table name matches the configured environment variable

## Next Steps

- [ ] Add GitHub Actions for CI/CD
- [ ] Implement auto-scaling based on CPU/memory
- [ ] Add CloudFront for global caching
- [ ] Set up CloudWatch alarms for failures
- [ ] Enable AWS X-Ray for distributed tracing
