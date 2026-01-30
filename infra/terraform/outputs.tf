output "kms_key_arn" {
  value = aws_kms_key.startracker.arn
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.observations.name
}