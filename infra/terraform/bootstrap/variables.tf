variable "region" {
  type        = string
  description = "AWS region for the state bucket and lock table."
  default     = "us-east-1"
}

variable "state_bucket_name" {
  type        = string
  description = "S3 bucket name for Terraform state."
  default     = "startracker-terraform-state"
}

variable "lock_table_name" {
  type        = string
  description = "DynamoDB table name for Terraform state locking."
  default     = "startracker-terraform-locks"
}
