output "dynamodb_table_name" {
  description = "DynamoDB table name"
  value       = aws_dynamodb_table.observations.name
}

output "ecr_repository_url" {
  description = "ECR repository URL"
  value       = aws_ecr_repository.startracker.repository_url
}

output "api_gateway_url" {
  description = "API Gateway invoke URL"
  value       = aws_apigatewayv2_stage.default.invoke_url
}

output "lambda_function_name" {
  description = "Lambda function name"
  value       = aws_lambda_function.startracker.function_name
}

output "ui_bucket_name" {
  description = "S3 bucket name for static UI hosting"
  value       = aws_s3_bucket.ui_site.id
}

output "ui_website_url" {
  description = "S3 static website URL for the UI"
  value       = "http://${aws_s3_bucket_website_configuration.ui_site.website_endpoint}"
}
