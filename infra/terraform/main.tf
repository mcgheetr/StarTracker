terraform {
  required_version = ">= 1.0"
  
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Remote state (uncomment after creating S3 bucket + DynamoDB table)
  # backend "s3" {
  #   bucket         = "startracker-terraform-state"
  #   key            = "startracker/terraform.tfstate"
  #   region         = "us-east-1"
  #   encrypt        = true
  #   dynamodb_table = "startracker-terraform-locks"
  # }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Project     = "StarTracker"
      Owner       = "todd"
      Purpose     = "portfolio"
      Environment = var.environment
      ManagedBy   = "Terraform"
      TTL         = "24h"
    }
  }
}

# DynamoDB table for observations
resource "aws_dynamodb_table" "observations" {
  name         = "startracker-observations-${var.environment}"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

  attribute {
    name = "Id"
    type = "S"
  }

  attribute {
    name = "Target"
    type = "S"
  }

  attribute {
    name = "ObservedAt"
    type = "N"
  }

  global_secondary_index {
    name            = "TargetIndex"
    hash_key        = "Target"
    range_key       = "ObservedAt"
    projection_type = "ALL"
  }

  server_side_encryption {
    enabled = true
  }

}
