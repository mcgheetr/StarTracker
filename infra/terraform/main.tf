terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region
}

# KMS key (symmetric) for envelope encryption
resource "aws_kms_key" "startracker" {
  description             = "KMS key for StarTracker field encryption"
  deletion_window_in_days = 30
  enable_key_rotation     = true
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

  server_side_encryption {
    enabled     = true
    kms_key_arn = aws_kms_key.startracker.arn
  }
}
