data "aws_caller_identity" "current" {}

resource "aws_s3_bucket" "ui_site" {
  bucket = "${var.app_name}-ui-${var.environment}-${data.aws_caller_identity.current.account_id}"

  tags = {
    Name = "${var.app_name}-ui-${var.environment}"
  }
}

resource "aws_s3_bucket_website_configuration" "ui_site" {
  bucket = aws_s3_bucket.ui_site.id

  index_document {
    suffix = "index.html"
  }

  error_document {
    key = "index.html"
  }
}

resource "aws_s3_bucket_ownership_controls" "ui_site" {
  bucket = aws_s3_bucket.ui_site.id

  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_public_access_block" "ui_site" {
  bucket = aws_s3_bucket.ui_site.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_acl" "ui_site" {
  bucket = aws_s3_bucket.ui_site.id
  acl    = "public-read"

  depends_on = [
    aws_s3_bucket_ownership_controls.ui_site,
    aws_s3_bucket_public_access_block.ui_site
  ]
}

resource "aws_s3_bucket_policy" "ui_site_public_read" {
  bucket = aws_s3_bucket.ui_site.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "PublicReadGetObject"
        Effect    = "Allow"
        Principal = "*"
        Action    = "s3:GetObject"
        Resource  = "${aws_s3_bucket.ui_site.arn}/*"
      }
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.ui_site]
}
