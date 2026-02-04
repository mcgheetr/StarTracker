# API Gateway HTTP API
resource "aws_apigatewayv2_api" "startracker" {
  name          = "${var.app_name}-${var.environment}"
  protocol_type = "HTTP"
  description   = "StarTracker API Gateway"

  cors_configuration {
    allow_origins = ["*"]
    allow_methods = ["GET", "POST", "OPTIONS"]
    allow_headers = ["content-type", "x-api-key"]
    max_age       = 300
  }

  tags = {
    Name = "${var.app_name}-apigw-${var.environment}"
  }
}

# Lambda integration and routes are defined in lambda.tf

# Stage (deployment)
resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.startracker.id
  name        = "$default"
  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      routeKey       = "$context.routeKey"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }

  tags = {
    Name = "${var.app_name}-apigw-stage-${var.environment}"
  }
}

# CloudWatch Log Group for API Gateway
resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apigateway/${var.app_name}-${var.environment}"
  retention_in_days = 7

  tags = {
    Name = "${var.app_name}-apigw-logs-${var.environment}"
  }
}
