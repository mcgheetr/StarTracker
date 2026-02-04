# ECR Repository
resource "aws_ecr_repository" "startracker" {
  name                 = "${var.app_name}-${var.environment}"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.app_name}-ecr-${var.environment}"
  }
}

# ECR Lifecycle Policy (keep last 10 images)
resource "aws_ecr_lifecycle_policy" "startracker" {
  repository = aws_ecr_repository.startracker.name

  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep last 10 images"
      selection = {
        tagStatus     = "any"
        countType     = "imageCountMoreThan"
        countNumber   = 10
      }
      action = {
        type = "expire"
      }
    }]
  })
}