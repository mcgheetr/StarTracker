# Terraform Bootstrap

Creates the S3 bucket and DynamoDB table used for Terraform remote state.

```powershell
cd infra/terraform/bootstrap
terraform init
terraform apply
```

Defaults:
- S3 bucket: `startracker-terraform-state`
- DynamoDB table: `startracker-terraform-locks`
- Region: `us-east-1`
