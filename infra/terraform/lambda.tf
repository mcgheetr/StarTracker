# Lambda-based Deployment (Free Tier)

resource "aws_lambda_function" "startracker" {
  function_name = "${var.app_name}-${var.environment}"
  role          = aws_iam_role.lambda.arn
  package_type  = "Image"
  image_uri     = "${aws_ecr_repository.startracker.repository_url}:${var.image_tag}"
  timeout       = 30
  memory_size   = 512

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT          = "Production"
      Repository__Type                = "DynamoDB"
      Repository__DynamoDB__Region    = var.region
      Repository__DynamoDB__TableName = aws_dynamodb_table.observations.name
      Encryption__UseAwsSdk           = "false"
      ApiKey                          = var.api_key
    }
  }

  tags = {
    Name = "${var.app_name}-lambda-${var.environment}"
  }
}

# Lambda IAM Role
resource "aws_iam_role" "lambda" {
  name = "${var.app_name}-lambda-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_basic" {
  role       = aws_iam_role.lambda.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy" "lambda_dynamodb" {
  name = "${var.app_name}-lambda-dynamodb-${var.environment}"
  role = aws_iam_role.lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:Query",
          "dynamodb:Scan"
        ]
        Resource = [
          aws_dynamodb_table.observations.arn,
          "${aws_dynamodb_table.observations.arn}/index/*"
        ]
      },
    ]
  })
}

# API Gateway integration
resource "aws_apigatewayv2_integration" "lambda" {
  api_id           = aws_apigatewayv2_api.startracker.id
  integration_type = "AWS_PROXY"
  integration_uri  = aws_lambda_function.startracker.invoke_arn
}

resource "aws_apigatewayv2_route" "lambda_default" {
  api_id    = aws_apigatewayv2_api.startracker.id
  route_key = "$default"
  target    = "integrations/${aws_apigatewayv2_integration.lambda.id}"
}

resource "aws_lambda_permission" "api_gateway" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.startracker.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.startracker.execution_arn}/*/*"
}

