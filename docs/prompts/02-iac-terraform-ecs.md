Design Terraform infrastructure for StarTracker targeting AWS.

Requirements:
- ECS Fargate service running a containerized .NET API.
- ECR repository for image.
- CloudWatch logs.
- DynamoDB table for observations.
- API Gateway HTTP API in front of the service (avoid ALB to reduce cost).
- VPC with public/private subnets (minimal but correct).
- Remote state: S3 backend + DynamoDB state lock table.
- Tagging: owner=todd, purpose=portfolio, env=dev, ttl=24h.

Constraints:
- Keep Terraform readable and minimal (no module explosion).
- Provide files split by concern: main/network/ecs/dynamo/iam/outputs.
- Provide variables for region, env, image tag, and API key secret reference.

Output:
1) File list + responsibilities
2) HCL code for each file
3) Apply/destroy commands
4) Notes on least-privilege IAM for ECS task role
